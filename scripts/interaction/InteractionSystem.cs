using Godot;

public partial class InteractionSystem : Node3D
{
	[Export]
	public float Reach { get; set; } = 2.0f;

	private BehaviorTracker _tracker;

	private CollisionObject3D _player;

	private IInteractable _target;

	private GodotObject _targetObject;

	private ulong _targetId;

	private double _targetSince;

	private static double Now =>
		Time.GetTicksMsec() / 1000.0;

	public override void _Ready()
	{
		_tracker =
			GetNode<BehaviorTracker>(
                "/root/BehaviorTracker"
			);

		Node node = GetParent();

		while (
			node != null &&
			node is not CollisionObject3D)
		{
			node = node.GetParent();
		}

		_player =
			node as CollisionObject3D;
	}

	public override void _PhysicsProcess(
		double delta)
	{
		Vector3 from =
			GlobalPosition;

		Vector3 to =
			from +
			(-GlobalTransform.Basis.Z) *
			Reach;

		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(
				from,
				to
			);

		if (_player != null)
		{
			query.Exclude =
				new Godot.Collections.Array<Rid>
				{
					_player.GetRid()
				};
		}

		var result =
			GetWorld3D()
				.DirectSpaceState
				.IntersectRay(query);

		IInteractable found =
			null;

		GodotObject foundObject =
			null;

		if (result.Count > 0)
		{
			foundObject =
				result["collider"]
					.AsGodotObject();

			if (foundObject
				is IInteractable interactable)
			{
				found = interactable;
			}
		}

		if (!ReferenceEquals(
				found,
				_target))
		{
			EndCurrentFocus(
				interacted: false
			);

			_target =
				found;

			_targetObject =
				foundObject;

			if (_targetObject != null)
			{
				_targetId =
					_targetObject
						.GetInstanceId();
			}
			else
			{
				_targetId = 0;
			}

			_targetSince =
				_target != null
					? Now
					: 0.0;

			if (_target != null)
			{
				_tracker
					.Interactions
					.ReportFocusStarted(
						_targetId
					);
			}
		}
	}

	public override void _UnhandledInput(
		InputEvent @event)
	{
		if (_target == null)
		{
			return;
		}

		if (!@event.IsActionPressed(
				"interact"))
		{
			return;
		}

		double hesitation =
			Now - _targetSince;

		_tracker.ReportInteraction(
			hesitation
		);

		_tracker.Interactions
			.ReportFocusEnded(
				_targetId,
				hesitation,
				interacted: true
			);

		_target.Interact(this);

		_target = null;
		_targetObject = null;
		_targetId = 0;
		_targetSince = 0.0;
	}

	private void EndCurrentFocus(
		bool interacted)
	{
		if (_target == null ||
			_targetObject == null)
		{
			return;
		}

		double duration =
			Now - _targetSince;

		_tracker.Interactions
			.ReportFocusEnded(
				_targetId,
				duration,
				interacted
			);

		_target = null;
		_targetObject = null;
		_targetId = 0;
		_targetSince = 0.0;
	}
}
