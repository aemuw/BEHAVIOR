using Godot;

//кожен фізичний кадр пускає промінь уперед
//і запам'ятовує, на що гравець дивиться
//по E взаємодіє з цим об'єктом
public partial class InteractionSystem : Node3D
{
	[Export]
	public float Reach { get; set; } = 2.0f;

	private BehaviorTracker _tracker;
	private CollisionObject3D _player;
	private IInteractable _target;
	private double _targetSince;

	private static double Now => Time.GetTicksMsec() / 1000.0;

	public override void _Ready()
	{
		_tracker = GetNode<BehaviorTracker>("/root/BehaviorTracker");

		//шукаємо гравця серед батьків, щоб промінь не потрапляв у його капсулу
		Node node = GetParent();
		while (node != null && node is not CollisionObject3D)
		{
			node = node.GetParent();
		}
		_player = node as CollisionObject3D;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 from = GlobalPosition;
		Vector3 to = from + (-GlobalTransform.Basis.Z) * Reach;

		var query = PhysicsRayQueryParameters3D.Create(from, to);
		if (_player != null)
		{
			query.Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
		}

		var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

		IInteractable found = null;
		if (result.Count > 0 && result["collider"].AsGodotObject() is IInteractable interactable)
		{
			found = interactable;
		}

		if (!ReferenceEquals(found, _target))
		{
			_target = found;
			_targetSince = Now;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_target != null && @event.IsActionPressed("interact"))
		{
			double hesitation = Now - _targetSince;
			_tracker.ReportInteraction(hesitation);
			_target.Interact(this);
		}
	}
}
