using System.Collections.Generic;
using Godot;

public partial class BehaviorDirector : Node
{
	private const int RequiredConsecutiveHits = 2;

	private const double GlobalEventCooldown = 8.0;

	private readonly List<BehaviorEvent> _events =
		new()
		{
			new AdaptiveDoorCloseEvent()
		};

	private BehaviorTracker _tracker;

	private BehaviorContext _lastContext;

	private int _consecutiveHits;

	private double _lastEventTime = -999.0;

	public int EventsTriggered { get; private set; }

	public string LastEventId { get; private set; } = "-";

	public int ConsecutiveHits =>
		_consecutiveHits;

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

	private void OnObservationCompleted(
		BehaviorObservationResult result)
	{
		UpdatePredictionStreak(result);

		if (!result.WasEvaluated ||
			!result.WasHit)
		{
			return;
		}

		if (result.Prediction is not Prediction prediction)
		{
			return;
		}

		if (!CanTriggerEvent())
		{
			return;
		}

		foreach (BehaviorEvent behaviorEvent
				 in _events)
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

			_consecutiveHits = 0;

			break;
		}
	}

	private void UpdatePredictionStreak(
		BehaviorObservationResult result)
	{
		if (!result.WasEvaluated ||
			!result.WasHit)
		{
			_consecutiveHits = 0;

			_lastContext =
				BehaviorContext.None;

			return;
		}

		if (_lastContext ==
			result.Context)
		{
			_consecutiveHits++;
		}
		else
		{
			_consecutiveHits = 1;

			_lastContext =
				result.Context;
		}

		if (_consecutiveHits >
			RequiredConsecutiveHits)
		{
			_consecutiveHits =
				RequiredConsecutiveHits;
		}
	}

	private bool CanTriggerEvent()
	{
		if (_consecutiveHits <
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
}
