using System.Collections.Generic;
using Godot;

//збирає поведінку гравця навколо інтерактивних об'єктів
public sealed class InteractionBehaviorTracker
{
	private const double QuickFocusThreshold = 0.25;
	private const double MeaningfulFocusThreshold = 0.75;
	private const double DeepFocusThreshold = 2.0;
	private const double DeliberateIgnoreThreshold = 1.0;

	private readonly HashSet<ulong> _seenTargets = new();
	private readonly HashSet<ulong> _interactedTargets = new();

	public int InteractionCount { get; private set; }

	public int DoorInteractionCount { get; private set; }

	public int FocusEvents { get; private set; }

	public int MeaningfulFocusEvents { get; private set; }

	public int DeepFocusEvents { get; private set; }

	public int QuickFocusEvents { get; private set; }

	public int IgnoredFocusEvents { get; private set; }

	public int UniqueTargetsSeen =>
		_seenTargets.Count;

	public int UniqueTargetsInteracted =>
		_interactedTargets.Count;

	public double TotalFocusTime { get; private set; }

	public double LastFocusTime { get; private set; }

	public double AverageFocusTime =>
		FocusEvents > 0
			? TotalFocusTime / FocusEvents
			: 0.0;

	public double MaxFocusTime { get; private set; }

	public double LastHesitation { get; private set; }

	private double _hesitationSum;

	public double AverageHesitation =>
		InteractionCount > 0
			? _hesitationSum / InteractionCount
			: 0.0;

	public double MaxHesitation { get; private set; }

	public int QuickInteractionCount { get; private set; }

	public int LongHesitationCount { get; private set; }

	public float MeaningfulFocusRate =>
		FocusEvents > 0
			? (float)MeaningfulFocusEvents / FocusEvents
			: 0.0f;

	public float DeliberateIgnoreRate =>
		FocusEvents > 0
			? (float)IgnoredFocusEvents / FocusEvents
			: 0.0f;

	public float DeepFocusRate =>
		FocusEvents > 0
			? (float)DeepFocusEvents / FocusEvents
			: 0.0f;

	public void ReportFocusStarted(
		ulong targetId)
	{
		_seenTargets.Add(targetId);
	}

	public void ReportFocusEnded(
		ulong targetId,
		double duration,
		bool interacted)
	{
		duration = Mathf.Max(
			(float)duration,
			0.0f
		);

		FocusEvents++;

		TotalFocusTime += duration;

		LastFocusTime = duration;

		MaxFocusTime =
			Mathf.Max(
				(float)MaxFocusTime,
				(float)duration
			);

		if (duration <=
			QuickFocusThreshold)
		{
			QuickFocusEvents++;
		}

		if (duration >=
			MeaningfulFocusThreshold)
		{
			MeaningfulFocusEvents++;
		}

		if (duration >=
			DeepFocusThreshold)
		{
			DeepFocusEvents++;
		}

		if (!interacted &&
			duration >=
			DeliberateIgnoreThreshold)
		{
			IgnoredFocusEvents++;
		}

		if (interacted)
		{
			_interactedTargets.Add(
				targetId
			);
		}
	}

	public void ReportInteraction(
		double hesitation)
	{
		hesitation = Mathf.Max(
			(float)hesitation,
			0.0f
		);

		InteractionCount++;

		LastHesitation =
			hesitation;

		MaxHesitation =
			Mathf.Max(
				(float)MaxHesitation,
				(float)hesitation
			);

		_hesitationSum +=
			hesitation;

		if (hesitation <=
			QuickFocusThreshold)
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
