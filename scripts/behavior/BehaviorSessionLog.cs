using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

//лог сесії для налаштування порогів і аналізу поведінки.
//F4 в грі або закриття вікна записує JSON в user://sessions/
//(Project -> Open User Data Folder)
public sealed class BehaviorSessionLog
{
	public sealed record ObservationEntry(
		double Time,
		BehaviorContext Context,
		BehaviorAction Actual,
		BehaviorAction? Predicted,
		float? Probability,
		float? Confidence,
		int Samples,
		bool Evaluated,
		bool Hit,
		double ReactionTime
	);

	public sealed record EventEntry(
		double Time,
		string EventId,
		string Reason
	);

	private static readonly JsonSerializerOptions JsonOptions =
		new()
		{
			WriteIndented = true,
			Converters = { new JsonStringEnumConverter() }
		};

	private readonly List<ObservationEntry> _observations = new();
	private readonly List<EventEntry> _events = new();

	public string LastSavePath { get; private set; } = "";

	public void RecordObservation(
		double time,
		BehaviorContext context,
		BehaviorAction actual,
		Prediction? prediction,
		bool evaluated,
		bool hit,
		double reactionTime)
	{
		_observations.Add(
			new ObservationEntry(
				Math.Round(time, 2),
				context,
				actual,
				prediction?.Action,
				prediction?.Probability,
				prediction?.Confidence,
				prediction?.Samples ?? 0,
				evaluated,
				hit,
				Math.Round(reactionTime, 2)
			)
		);
	}

	public void RecordEvent(
		double time,
		string eventId,
		string reason)
	{
		_events.Add(
			new EventEntry(
				Math.Round(time, 2),
				eventId,
				reason
			)
		);
	}

	public string Save(BehaviorTracker tracker)
	{
		try
		{
			string directory =
				ProjectSettings.GlobalizePath("user://sessions");

			System.IO.Directory.CreateDirectory(directory);

			string path =
				System.IO.Path.Combine(
					directory,
					$"session_{DateTime.Now:yyyyMMdd_HHmmss}.json"
				);

			var data =
				new
				{
					Summary = BuildSummary(tracker),
					Observations = _observations,
					Events = _events
				};

			System.IO.File.WriteAllText(
				path,
				JsonSerializer.Serialize(data, JsonOptions)
			);

			LastSavePath = path;

			GD.Print($"[BEHAVIOR] Session log saved: {path}");

			return path;
		}
		catch (Exception exception)
		{
			GD.PrintErr($"[BEHAVIOR] Failed to save session log: {exception.Message}");

			return "";
		}
	}

	private static object BuildSummary(
		BehaviorTracker t)
	{
		return new
		{
			TotalTime = Math.Round(t.TotalTime, 1),

			Movement = new
			{
				WalkTime = Math.Round(t.Movement.WalkTime, 1),
				RunTime = Math.Round(t.Movement.RunTime, 1),
				StandStillTime = Math.Round(t.Movement.StandStillTime, 1),
				Distance = t.Movement.DistanceWalked,
				Reversals = t.Movement.DirectionReversalCount,
				SprintStarts = t.Movement.SprintStartCount
			},

			Look = new
			{
				LargeTurns = t.Look.LargeTurnCount,
				YawTravel = t.Look.TotalYawTravel
			},

			Interactions = new
			{
				Total = t.Interactions.InteractionCount,
				Doors = t.Interactions.DoorInteractionCount,
				AverageHesitation = t.Interactions.AverageHesitation,
				UniqueSeen = t.Interactions.UniqueTargetsSeen,
				UniqueInteracted = t.Interactions.UniqueTargetsInteracted
			},

			Rooms = new
			{
				Entries = t.Rooms.RoomEntries,
				DarkEntries = t.Rooms.DarkEntries,
				LitEntries = t.Rooms.LitEntries,
				Returns = t.Rooms.ReturnVisits,
				DarkTime = Math.Round(t.Rooms.DarkTime, 1),
				LitTime = Math.Round(t.Rooms.LitTime, 1),
				Visited = t.Rooms.Visited.Values
					.Select(room => new
					{
						room.Id,
						room.IsDark,
						room.Entries,
						TimeInside = Math.Round(room.TimeInside, 1)
					})
					.ToList()
			},

			Profile = new
			{
				Exploration = Metric(t.Profile.Exploration),
				Curiosity = Metric(t.Profile.Curiosity),
				Avoidance = Metric(t.Profile.Avoidance),
				Repetition = Metric(t.Profile.Repetition),
				Predictability = Metric(t.Profile.Predictability),
				Hesitation = Metric(t.Profile.Hesitation),
				DarkAvoidance = Metric(t.Profile.DarkAvoidance)
			},

			Model = new
			{
				t.Model.Evaluations,
				t.Model.Hits,
				Predictability = t.Model.Predictability,
				Recent = t.Model.RecentPredictability,
				AboveChance = t.Model.PredictabilityAboveChance
			}
		};
	}

	private static object Metric(BehaviorMetric metric)
	{
		return new
		{
			metric.Value,
			metric.Confidence
		};
	}
}
