using Godot;

//коли гравець входить в нову кімнату, предмет у кімнаті, яку він щойно залишив,
//непомітно зсувається. Спрацьовує не на кожен вхід і не потребує
//влучного прогнозу моделі.
public sealed class ObjectShiftEvent : BehaviorEvent
{
	private const double MinimumSessionTime = 30.0;
	private const double MinimumRoomTime = 6.0;

	public override string Id =>
		"object_shift";

	public override int Priority =>
		70;

	public override bool RequiresPredictionHit =>
		false;

	public override float Chance =>
		0.55f;

	public override double Cooldown =>
		40.0;

	public override bool CanExecute(
		BehaviorObservationResult result,
		BehaviorTracker tracker)
	{
		if (result.Context != BehaviorContext.RoomEntered)
		{
			return false;
		}

		if (tracker.TotalTime < MinimumSessionTime)
		{
			return false;
		}

		return FindShiftableProp(tracker) != null;
	}

	public override void Execute(
		BehaviorObservationResult result,
		BehaviorTracker tracker)
	{
		FindShiftableProp(tracker)?.Shift();
	}

	private static MovableProp FindShiftableProp(
		BehaviorTracker tracker)
	{
		string roomId =
			tracker.Rooms.PreviousRoomId;

		if (string.IsNullOrEmpty(roomId) ||
			roomId == tracker.Rooms.CurrentRoomId)
		{
			return null;
		}

		RoomBehaviorTracker.RoomStats stats =
			tracker.Rooms.GetStats(roomId);

		if (stats == null ||
			stats.TimeInside < MinimumRoomTime)
		{
			return null;
		}

		Camera3D camera =
			tracker.GetViewport().GetCamera3D();

		foreach (Node node in
			tracker.GetTree().GetNodesInGroup(MovableProp.GroupName))
		{
			if (node is not MovableProp prop ||
				!GodotObject.IsInstanceValid(prop))
			{
				continue;
			}

			if (prop.RoomId != roomId ||
				!prop.CanShift)
			{
				continue;
			}

			if (VisibilityUtil.IsPointVisible(
					camera,
					prop.GlobalPosition))
			{
				continue;
			}

			return prop;
		}

		return null;
	}
}
