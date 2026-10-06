using Godot;

public partial class PlayerController : CharacterBody3D
{
	[Export]
	public float MoveSpeed { get; set; } = 3.0f;

	[Export]
	public float MouseSensitivity { get; set; } = 0.0025f;

	private Node3D _head;
	private Camera3D _camera;

	private float _pitch;

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion &&
			Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);

			_pitch -= mouseMotion.Relative.Y * MouseSensitivity;
			_pitch = Mathf.Clamp(
				_pitch,
				Mathf.DegToRad(-85.0f),
				Mathf.DegToRad(85.0f)
			);

			_head.Rotation = new Vector3(_pitch, 0.0f, 0.0f);
		}

		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.Pressed &&
			Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 input = Input.GetVector(
			"move_left",
			"move_right",
			"move_forward",
            "move_backward"
		);

		Vector3 direction = new Vector3(input.X, 0.0f, input.Y);

		if (direction.LengthSquared() > 1.0f)
		{
			direction = direction.Normalized();
		}

		direction = GlobalTransform.Basis * direction;
		direction.Y = 0.0f;

		Velocity = direction * MoveSpeed;

		MoveAndSlide();
	}
}
