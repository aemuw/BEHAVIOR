using System.Collections.Generic;
using System.Linq;
using Godot;

//Director вирішує, чи варто запускати подію, коли завершилось спостереження.
//Події бувають двох типів: ті, що потребують серії влучних прогнозів моделі,
//і "фонові" (наприклад, зсув предмета), яким достатньо контексту.
public partial class BehaviorDirector : Node
{
	private const int RequiredConsecutiveHits = 2;

	private const double GlobalEventCooldown = 12.0;

	private readonly List<BehaviorEvent> _events = new()
	{
		new AdaptiveDoorCloseEvent(),
		new ObjectShiftEvent(),
		new AdaptiveLightFlickerEvent()
	};

	private readonly Dictionary<BehaviorContext, int> _hitStreaks = new();

	private readonly Dictionary<string, double> _lastRunById = new();

	private readonly RandomNumberGenerator _rng = new();

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
		_rng.Randomize();

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
		LastContext = result.Context;
		LastActualAction = result.ActualAction;
		LastPredictedAction =
			result.Prediction?.Action
			?? BehaviorAction.Idle;

		UpdatePredictionStreak(result);

		bool hit =
			result.WasEvaluated &&
			result.WasHit;

		bool streakReady =
			GetHitStreak(result.Context) >=
			RequiredConsecutiveHits;

		string baseDecision =
			!result.WasEvaluated
				? "NO EVALUATION"
				: hit
					? "HIT"
					: "MISS";

		string baseReason =
			!result.WasEvaluated
				? "Not enough samples"
				: hit
					? $"Streak {GetHitStreak(result.Context)}/{RequiredConsecutiveHits}"
					: $"Expected {result.Prediction?.Action}, got {result.ActualAction}";

		double sinceLastEvent =
			_tracker.TotalTime - _lastEventTime;

		if (sinceLastEvent < GlobalEventCooldown)
		{
			LastDecision = $"{baseDecision} / COOLDOWN";

			LastDecisionReason =
				$"Global cooldown {GlobalEventCooldown - sinceLastEvent:F1}s";

			return;
		}

		string reason = baseReason;

		foreach (BehaviorEvent behaviorEvent in
			_events.OrderByDescending(e => e.Priority))
		{
			if (behaviorEvent.RequiresPredictionHit &&
				!(hit && streakReady))
			{
				continue;
			}

			if (_lastRunById.TryGetValue(
					behaviorEvent.Id,
					out double lastRun) &&
				_tracker.TotalTime - lastRun <
				behaviorEvent.Cooldown)
			{
				reason = $"{behaviorEvent.Id}: own cooldown";
				continue;
			}

			if (!behaviorEvent.CanExecute(
					result,
					_tracker))
			{
				continue;
			}

			//навмисна випадковість: не кожного разу, коли умови виконані
			if (_rng.Randf() > behaviorEvent.Chance)
			{
				reason = $"{behaviorEvent.Id}: skipped by chance";
				continue;
			}

			behaviorEvent.Execute(
				result,
				_tracker
			);

			EventsTriggered++;

			LastEventId = behaviorEvent.Id;

			_lastEventTime = _tracker.TotalTime;

			_lastRunById[behaviorEvent.Id] = _tracker.TotalTime;

			LastDecision = "EVENT TRIGGERED";

			LastDecisionReason = behaviorEvent.Id;

			if (behaviorEvent.RequiresPredictionHit)
			{
				_hitStreaks[result.Context] = 0;
			}

			_tracker.SessionLog.RecordEvent(
				_tracker.TotalTime,
				behaviorEvent.Id,
				$"{result.Context}: predicted {result.Prediction?.Action}, actual {result.ActualAction}"
			);

			return;
		}

		LastDecision = $"{baseDecision} / NO EVENT";

		LastDecisionReason = reason;
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
			GetHitStreak(context) + 1;

		_hitStreaks[context] =
			Mathf.Min(
				streak,
				RequiredConsecutiveHits
			);
	}
}
