using Godot;

public readonly record struct BehaviorMetric(
	float Value,
	float Confidence
);

public sealed class BehaviorProfile
{
	public BehaviorMetric Exploration { get; private set; }

	public BehaviorMetric Curiosity { get; private set; }

	public BehaviorMetric Avoidance { get; private set; }

	public BehaviorMetric Repetition { get; private set; }

	public BehaviorMetric Predictability { get; private set; }

	public BehaviorMetric Hesitation { get; private set; }

	public float OverallConfidence
	{
		get
		{
			float total =
				Exploration.Confidence +
				Curiosity.Confidence +
				Avoidance.Confidence +
				Repetition.Confidence +
				Predictability.Confidence +
				Hesitation.Confidence;

			return total / 6.0f;
		}
	}

	public void Recalculate(
		MovementBehaviorTracker movement,
		LookBehaviorTracker look,
		SpatialBehaviorTracker spatial,
		InteractionBehaviorTracker interactions,
		BehaviorModel model)
	{
		Exploration =
			new BehaviorMetric(
				CalculateExploration(spatial),
				CalculateSpatialConfidence(spatial)
			);

		Curiosity =
			new BehaviorMetric(
				CalculateCuriosity(interactions),
				CalculateInteractionConfidence(
					interactions
				)
			);

		Avoidance =
			new BehaviorMetric(
				CalculateAvoidance(interactions),
				CalculateInteractionConfidence(
					interactions
				)
			);

		Repetition =
			new BehaviorMetric(
				CalculateRepetition(
					spatial,
					model
				),
				CalculateRepetitionConfidence(
					spatial,
					model
				)
			);

		Predictability =
			new BehaviorMetric(
				CalculatePredictability(model),
				CalculatePredictionConfidence(model)
			);

		Hesitation =
			new BehaviorMetric(
				CalculateHesitation(interactions),
				CalculateInteractionConfidence(
					interactions
				)
			);
	}

	private static float CalculateExploration(
		SpatialBehaviorTracker spatial)
	{
		return Mathf.Clamp(
			spatial.NoveltyRate,
			0.0f,
			1.0f
		);
	}

	private static float CalculateCuriosity(
		InteractionBehaviorTracker interactions)
	{
		if (interactions.FocusEvents == 0)
		{
			return 0.0f;
		}

		float meaningful =
			interactions.MeaningfulFocusRate;

		float deep =
			interactions.DeepFocusRate;

		float avoidance =
			interactions.DeliberateIgnoreRate;

		float value =
			meaningful * 0.60f +
			deep * 0.40f;

		value *=
			1.0f -
			avoidance * 0.35f;

		return Mathf.Clamp(
			value,
			0.0f,
			1.0f
		);
	}

	private static float CalculateAvoidance(
		InteractionBehaviorTracker interactions)
	{
		return Mathf.Clamp(
			interactions.DeliberateIgnoreRate,
			0.0f,
			1.0f
		);
	}

	private static float CalculateRepetition(
		SpatialBehaviorTracker spatial,
		BehaviorModel model)
	{
		float spatialRepetition =
			spatial.BacktrackingTendency;

		float behavioralPredictability =
			model.Predictability;

		return Mathf.Clamp(
			spatialRepetition * 0.60f +
			behavioralPredictability * 0.40f,
			0.0f,
			1.0f
		);
	}

	private static float CalculatePredictability(
		BehaviorModel model)
	{
		if (model.Evaluations == 0)
		{
			return 0.0f;
		}

		//Recent accuracy важливіша за всю історію,
		//бо звички гравця можуть змінюватися
		return Mathf.Clamp(
			model.RecentPredictability,
			0.0f,
			1.0f
		);
	}

	private static float CalculateHesitation(
		InteractionBehaviorTracker interactions)
	{
		if (interactions.InteractionCount == 0)
		{
			return 0.0f;
		}

		//2.5 секунди — умовна верхня межа
		//"сильної" затримки для поточного прототипу.
		return Mathf.Clamp(
			(float)
				interactions.AverageHesitation /
				2.5f,
			0.0f,
			1.0f
		);
	}

	private static float CalculateSpatialConfidence(
		SpatialBehaviorTracker spatial)
	{
		return ConfidenceFromSamples(
			spatial.Samples,
			30.0f
		);
	}

	private static float CalculateInteractionConfidence(
		InteractionBehaviorTracker interactions)
	{
		return ConfidenceFromSamples(
			interactions.FocusEvents,
			10.0f
		);
	}

	private static float CalculatePredictionConfidence(
		BehaviorModel model)
	{
		return ConfidenceFromSamples(
			model.Evaluations,
			10.0f
		);
	}

	private static float CalculateRepetitionConfidence(
		SpatialBehaviorTracker spatial,
		BehaviorModel model)
	{
		int samples =
			Mathf.Max(
				spatial.Samples,
				model.Evaluations
			);

		return ConfidenceFromSamples(
			samples,
			30.0f
		);
	}

	private static float ConfidenceFromSamples(
		int samples,
		float scale)
	{
		if (samples <= 0)
		{
			return 0.0f;
		}

		return Mathf.Clamp(
			1.0f -
			Mathf.Exp(
				-samples / scale
			),
			0.0f,
			1.0f
		);
	}
}
