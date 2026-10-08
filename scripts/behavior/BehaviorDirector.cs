using System.Collections.Generic;
using Godot;
using System.Linq;

public partial class BehaviorDirector : Node
{
	private const int RequiredConsecutiveHits = 2;

	private const double GlobalEventCooldown = 8.0;

	private readonly List<BehaviorEvent> _events = new()
		{
			new AdaptiveDoorCloseEvent(),
			new AdaptiveLightFlickerEvent()
		};

	private readonly Dictionary<
		BehaviorContext,
		int
	> _hitStreaks = new();

	private BehaviorTracker _tracker;

	private double _lastEventTime = -999.0;

	public int EventsTriggered { get; private set; }

	public string LastEventId { get; private set; } = "-";

	public BehaviorContext LastContext { get; private set; }
		= BehaviorContext.None;

	public BehaviorAction LastPredictedAction { get; private set; }
		= BehaviorAction.Idle;

	public BehaviorAction LastActualAction { get; private set; }
		= BehaviorAction.Idle;

	public string LastDecision { get; private set; }
		= "Waiting for observation";

	public string LastDecisionReason { get; private set; }
		= "-";

	public int ConsecutiveHits =>
		GetHitStreak(LastContext);

	public override void _Ready()
	{
		_tracker =
			GetNode<BehaviorTracker>(
				"/root/BehaviorTracker"
			);

		_tracker.ObservationCompleted +=
			OnObservationCompleted;
	}

	public override void _ExitTree()
	{
		if (_tracker != null)
		{
			_tracker.ObservationCompleted -=
				OnObservationCompleted;
		}
	}

	public int GetHitStreak(
		BehaviorContext context)
	{
		return _hitStreaks.TryGetValue(
			context,
			out int streak
		)
			? streak
			: 0;
	}

	private void OnObservationCompleted(
		BehaviorObservationResult result)
	{
		LastContext =
			result.Context;

		LastActualAction =
			result.ActualAction;

		LastPredictedAction =
			result.Prediction?.Action
			?? BehaviorAction.Idle;

		UpdatePredictionStreak(
			result
		);

		if (!result.WasEvaluated)
		{
			LastDecision =
				"NO EVALUATION";

			LastDecisionReason =
				"Not enough samples";

			return;
		}

		if (!result.WasHit)
		{
			LastDecision =
				"MISS";

			LastDecisionReason =
				$"Expected {result.Prediction?.Action}, " +
				$"got {result.ActualAction}";

			return;
		}

		if (!CanTriggerEvent(
				result.Context))
		{
			LastDecision =
				"HIT / NO EVENT";

			LastDecisionReason =
				BuildCooldownReason(
					result.Context
				);

			return;
		}

		foreach (BehaviorEvent behaviorEvent
			 in _events
				 .OrderByDescending(
					 e => e.Priority
				 ))
		{
			if (!behaviorEvent.CanExecute(
					result,
					_tracker))
			{
				continue;
			}

			behaviorEvent.Execute(
				result,
				_tracker
			);

			EventsTriggered++;

			LastEventId =
				behaviorEvent.Id;

			_lastEventTime =
				_tracker.TotalTime;

			LastDecision =
				"EVENT TRIGGERED";

			LastDecisionReason =
				behaviorEvent.Id;

			_hitStreaks[result.Context] = 0;

			return;
		}

		LastDecision =
			"HIT / EVENT BLOCKED";

		LastDecisionReason =
			"BehaviorEvent conditions not satisfied";
	}

	private void UpdatePredictionStreak(
		BehaviorObservationResult result)
	{
		if (!result.WasEvaluated)
		{
			return;
		}

		BehaviorContext context =
			result.Context;

		if (!result.WasHit)
		{
			_hitStreaks[context] = 0;

			return;
		}

		int streak =
			GetHitStreak(context);

		streak++;

		if (streak >
			RequiredConsecutiveHits)
		{
			streak =
				RequiredConsecutiveHits;
		}

		_hitStreaks[context] =
			streak;
	}

	private bool CanTriggerEvent(
		BehaviorContext context)
	{
		if (GetHitStreak(context) <
			RequiredConsecutiveHits)
		{
			return false;
		}

		if (_tracker.TotalTime -
			_lastEventTime <
			GlobalEventCooldown)
		{
			return false;
		}

		return true;
	}

	private string BuildCooldownReason(
		BehaviorContext context)
	{
		int streak =
			GetHitStreak(context);

		if (streak <
			RequiredConsecutiveHits)
		{
			return
				$"Streak {streak}/" +
				$"{RequiredConsecutiveHits}";
		}

		double remaining =
			GlobalEventCooldown -
			(
				_tracker.TotalTime -
				_lastEventTime
			);

		if (remaining > 0.0)
		{
			return
				$"Cooldown {remaining:F1}s";
		}

		return "No event matched";
	}
}
