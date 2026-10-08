using Godot;

public partial class Door : AnimatableBody3D, IInteractable
{
	[Export]
	public float OpenAngleDegrees { get; set; } = 90.0f;

	[Export]
	public float OpenTime { get; set; } = 0.6f;

	//1 або -1: в який бік відчиняються двері
	[Export]
	public int OpenDirection { get; set; } = 1;

	public bool IsOpen { get; private set; }

	private Tween _tween;
	private BehaviorTracker _tracker;

	//поворот зачинених дверей (залежить від того, як двері поставлені в сцені)
	private float _closedYaw;

	//поточний поворот, який ми ведемо самі, щоб tween не плутав
	//еквівалентні кути (наприклад -180 і 180 градусів)
	private float _yaw;

	//збільшується при кожній взаємодії гравця
	private int _interactionVersion;

	public override void _Ready()
	{
		_tracker =
			GetNode<BehaviorTracker>(
				"/root/BehaviorTracker"
			);

		_closedYaw = Rotation.Y;
		_yaw = _closedYaw;
	}

	public void Interact(Node3D interactor)
	{
		_interactionVersion++;

		SetOpen(!IsOpen);

		_tracker.ReportDoorInteraction(
			IsOpen,
			this
		);
	}

	public void SetOpen(bool open)
	{
		IsOpen = open;

		float direction =
			OpenDirection >= 0
				? 1.0f
				: -1.0f;

		float targetYaw =
			open
				? _closedYaw +
				  Mathf.DegToRad(OpenAngleDegrees) *
				  direction
				: _closedYaw;

		_tween?.Kill();

		_tween = CreateTween();

		_tween.TweenMethod(
			Callable.From<float>(ApplyYaw),
			_yaw,
			targetYaw,
			OpenTime
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);
	}

	//подія гри: двері зачиняються самі, але лише якщо гравець
	//не торкався їх за цей час
	public void ScheduleAdaptiveClose(
		double delay = 1.2)
	{
		if (!IsOpen)
		{
			return;
		}

		int version = _interactionVersion;

		GetTree()
			.CreateTimer(delay)
			.Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(this) ||
					!IsInsideTree())
				{
					return;
				}

				if (version != _interactionVersion ||
					!IsOpen)
				{
					return;
				}

				SetOpen(false);
			};
	}

	private void ApplyYaw(float yaw)
	{
		_yaw = yaw;

		Rotation =
			new Vector3(
				0.0f,
				yaw,
				0.0f
			);
	}
}
