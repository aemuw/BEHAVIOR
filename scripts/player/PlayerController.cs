using Godot;

public partial class PlayerController : CharacterBody3D
{
	[Export]
	public float MoveSpeed { get; set; } = 2.5f;

	[Export]
	public float RunSpeed { get; set; } = 4.5f;

	[Export]
	public float MouseSensitivity { get; set; } = 0.0025f;

	private Node3D _head;
	private Camera3D _camera;
	private BehaviorTracker _tracker;

	private float _pitch;

	public override void _Ready()
	{
		_head =
			GetNode<Node3D>("Head");

		_camera =
			GetNode<Camera3D>(
                "Head/Camera3D"
			);

		_tracker =
			GetNode<BehaviorTracker>(
                "/root/BehaviorTracker"
			);

		Input.MouseMode =
			Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(
		InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion &&
			Input.MouseMode ==
			Input.MouseModeEnum.Captured)
		{
			float yaw =
				-mouseMotion.Relative.X *
				MouseSensitivity;

			float pitchDelta =
				-mouseMotion.Relative.Y *
				MouseSensitivity;

			RotateY(yaw);

			_tracker.ReportLook(
				yaw,
				pitchDelta
			);

			_pitch += pitchDelta;

			_pitch =
				Mathf.Clamp(
					_pitch,
					Mathf.DegToRad(-85.0f),
					Mathf.DegToRad(85.0f)
				);

			_head.Rotation =
				new Vector3(
					_pitch,
					0.0f,
					0.0f
				);
		}

		if (@event.IsActionPressed(
				"ui_cancel"))
		{
			Input.MouseMode =
				Input.MouseModeEnum.Visible;
		}

		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.Pressed &&
			Input.MouseMode ==
			Input.MouseModeEnum.Visible)
		{
			Input.MouseMode =
				Input.MouseModeEnum.Captured;
		}
	}

	public override void _PhysicsProcess(
		double delta)
	{
		Vector2 input =
			Input.GetVector(
				"move_left",
				"move_right",
				"move_forward",
                "move_backward"
			);

		bool sprinting =
			input != Vector2.Zero &&
			Input.IsActionPressed("sprint");

		float speed =
			sprinting
				? RunSpeed
				: MoveSpeed;

		Vector3 localDirection =
			new Vector3(
				input.X,
				0.0f,
				input.Y
			);

		if (localDirection.LengthSquared() >
			1.0f)
		{
			localDirection =
				localDirection.Normalized();
		}

		Vector3 worldDirection =
			GlobalTransform.Basis *
			localDirection;

		worldDirection.Y = 0.0f;

		if (worldDirection.LengthSquared() >
			0.0001f)
		{
			worldDirection =
				worldDirection.Normalized();
		}

		Velocity =
			worldDirection * speed;

		MoveAndSlide();

		float actualSpeed =
			new Vector2(
				Velocity.X,
				Velocity.Z
			).Length();

		float forwardSpeed =
			Velocity.Dot(
				-GlobalTransform.Basis.Z
			);

		float lateralSpeed =
			Velocity.Dot(
				GlobalTransform.Basis.X
			);

		_tracker.ReportMovement(
			delta,
			actualSpeed,
			sprinting,
			forwardSpeed,
			lateralSpeed
		);
	}
}
