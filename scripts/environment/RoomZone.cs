using Godot;

//зона кімнати. Повідомляє BehaviorTracker, коли гравець входить і виходить.
//Кімната вважається темною, якщо в ній немає AdaptiveLight
//або її базова яскравість дуже мала.
//Світло кімнати - дочірній вузол цієї зони.
public partial class RoomZone : Area3D
{
	private const float DarkEnergyThreshold = 0.15f;

	[Export]
	public string RoomId { get; set; } = "room";

	public AdaptiveLight Light { get; private set; }

	public bool IsDark =>
		Light == null ||
		!Light.Visible ||
		Light.BaseEnergy < DarkEnergyThreshold;

	private BehaviorTracker _tracker;

	public override void _Ready()
	{
		_tracker =
			GetNode<BehaviorTracker>(
				"/root/BehaviorTracker"
			);

		foreach (Node child in GetChildren())
		{
			if (child is AdaptiveLight light)
			{
				Light = light;
			}
		}

		_tracker.Rooms.RegisterRoom(
			RoomId,
			IsDark
		);

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (!body.IsInGroup("player"))
		{
			return;
		}

		_tracker.ReportRoomEntered(
			RoomId,
			IsDark
		);
	}

	private void OnBodyExited(Node3D body)
	{
		if (!body.IsInGroup("player"))
		{
			return;
		}

		_tracker.ReportRoomExited(
			RoomId
		);
	}
}
