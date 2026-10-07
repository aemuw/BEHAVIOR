using Godot;

//метрики взаємодії та вагання
public sealed class InteractionBehaviorTracker
{
	public int InteractionCount { get; private set; }
	public int DoorInteractionCount { get; private set; }

	public int QuickInteractionCount { get; private set; }
	public int LongHesitationCount { get; private set; }

	public double LastHesitation { get; private set; }

	public double AverageHesitation =>
		InteractionCount > 0
			? _hesitationSum / InteractionCount
			: 0.0;

	public double MaxHesitation { get; private set; }

	private double _hesitationSum;

	public void ReportInteraction(double hesitation)
	{
		hesitation = Mathf.Max(
			(float)hesitation,
			0.0f
		);

		InteractionCount++;

		LastHesitation = hesitation;

		MaxHesitation = Mathf.Max(
			(float)MaxHesitation,
			(float)hesitation
		);

		_hesitationSum += hesitation;

		if (hesitation <= 0.35)
		{
			QuickInteractionCount++;
		}
		else if (hesitation >= 2.0)
		{
			LongHesitationCount++;
		}
	}

	public void ReportDoorInteraction()
	{
		DoorInteractionCount++;
	}
}
