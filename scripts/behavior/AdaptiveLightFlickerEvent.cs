using Godot;

public sealed class AdaptiveLightFlickerEvent : BehaviorEvent
{
	public override int Priority =>
		50;
	
	private const float RequiredCuriosity = 0.45f;

	private const float RequiredConfidence = 0.40f;

	private const double MinimumSessionTime = 20.0;

	public override string Id =>
		"adaptive_light_flicker";

	public override bool CanExecute(
		BehaviorObservationResult result,
		BehaviorTracker tracker)
	{
		if (!result.WasEvaluated ||
			!result.WasHit)
		{
			return false;
		}

		if (tracker.TotalTime <
			MinimumSessionTime)
		{
			return false;
		}

		BehaviorMetric curiosity =
			tracker.Profile.Curiosity;

		if (curiosity.Value <
			RequiredCuriosity)
		{
			return false;
		}

		if (curiosity.Confidence <
			RequiredConfidence)
		{
			return false;
		}

		AdaptiveLight light =
			FindAvailableLight(tracker);

		return light != null;
	}

	public override void Execute(
		BehaviorObservationResult result,
		BehaviorTracker tracker)
	{
		AdaptiveLight light =
			FindAvailableLight(tracker);

		if (light == null)
		{
			return;
		}

		light.TriggerFlicker();
	}

	private static AdaptiveLight FindAvailableLight(
		BehaviorTracker tracker)
	{
		Godot.Collections.Array<Node> nodes =
			tracker.GetTree().GetNodesInGroup(
                "behavior_adaptive_light"
			);

		foreach (Node node in nodes)
		{
			if (node is AdaptiveLight light &&
				GodotObject.IsInstanceValid(light) &&
				!light.IsFlickering)
			{
				return light;
			}
		}

		return null;
	}
}
