using Godot;

public sealed class AdaptiveLightFlickerEvent : BehaviorEvent
{
	public override int Priority =>
		50;

	public override float Chance =>
		0.60f;

	public override double Cooldown =>
		25.0;

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

		return FindAvailableLight(tracker) != null;
	}

	public override void Execute(
		BehaviorObservationResult result,
		BehaviorTracker tracker)
	{
		AdaptiveLight light =
			FindAvailableLight(tracker);

		light?.TriggerFlicker();
	}

	//мерехтить найближча до гравця лампа, а не перша в списку
	private static AdaptiveLight FindAvailableLight(
		BehaviorTracker tracker)
	{
		Node player =
			tracker.GetTree().GetFirstNodeInGroup("player");

		Vector3 origin =
			player is Node3D playerBody
				? playerBody.GlobalPosition
				: Vector3.Zero;

		AdaptiveLight best = null;
		float bestDistance = float.MaxValue;

		foreach (Node node in
			tracker.GetTree().GetNodesInGroup(
				"behavior_adaptive_light"))
		{
			if (node is not AdaptiveLight light ||
				!GodotObject.IsInstanceValid(light) ||
				light.IsFlickering ||
				!light.Visible)
			{
				continue;
			}

			float distance =
				light.GlobalPosition.DistanceTo(origin);

			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = light;
			}
		}

		return best;
	}
}
