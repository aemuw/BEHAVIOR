using Godot;

public sealed class AdaptiveDoorCloseEvent : BehaviorEvent
{
	public override int Priority =>
		100;
	
	private const float RequiredPredictionProbability = 0.60f;

	private const float RequiredPredictionConfidence = 0.50f;

	private const int RequiredContextEvaluations = 3;

	private const float RequiredRecentAccuracy = 0.70f;

	public override string Id =>
		"adaptive_door_close";

	public override float Chance =>
		0.70f;

	public override double Cooldown =>
		30.0;

	public override bool CanExecute(
		BehaviorObservationResult result,
		BehaviorTracker tracker)
	{
		if (result.Context !=
			BehaviorContext.DoorOpened)
		{
			return false;
		}

		if (!result.WasEvaluated ||
			!result.WasHit)
		{
			return false;
		}

		if (!result.Prediction.HasValue)
		{
			return false;
		}

		Prediction prediction =
			result.Prediction.Value;

		if (prediction.Probability <
			RequiredPredictionProbability)
		{
			return false;
		}

		if (prediction.Confidence <
			RequiredPredictionConfidence)
		{
			return false;
		}

		int evaluations =
			tracker.Model.GetEvaluations(
				BehaviorContext.DoorOpened
			);

		if (evaluations <
			RequiredContextEvaluations)
		{
			return false;
		}

		float recentAccuracy =
			tracker.Model.GetRecentPredictability(
				BehaviorContext.DoorOpened
			);

		if (recentAccuracy <
			RequiredRecentAccuracy)
		{
			return false;
		}

		Door door =
			tracker.LastInteractedDoor;

		if (door == null ||
			!GodotObject.IsInstanceValid(door))
		{
			return false;
		}

		return door.IsOpen;
	}

	public override void Execute(
		BehaviorObservationResult result,
		BehaviorTracker tracker)
	{
		Door door =
			tracker.LastInteractedDoor;

		if (door == null ||
			!GodotObject.IsInstanceValid(door) ||
			!door.IsOpen)
		{
			return;
		}

		door.ScheduleAdaptiveClose();
	}
}
