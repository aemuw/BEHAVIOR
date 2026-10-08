using Godot;

public sealed class AdaptiveDoorCloseEvent : BehaviorEvent
{
	private const float RequiredPredictionProbability = 0.60f;

	private const float RequiredPredictionConfidence = 0.55f;

	private const float RequiredProfilePredictability = 0.60f;

	private const float RequiredProfileConfidence = 0.45f;

	public override string Id =>
		"adaptive_door_close";

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

		if (tracker.Profile.Predictability.Value <
			RequiredProfilePredictability)
		{
			return false;
		}

		if (tracker.Profile.Predictability.Confidence <
			RequiredProfileConfidence)
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

		if (!door.IsOpen)
		{
			return false;
		}

		return true;
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
