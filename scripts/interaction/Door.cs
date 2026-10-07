using Godot;

public partial class Door : AnimatableBody3D, IInteractable
{
	[Export]
	public float OpenAngleDegrees { get; set; } = 90.0f;

	[Export]
	public float OpenTime { get; set; } = 0.6f;

	public bool IsOpen { get; private set; }

	private Tween _tween;
	private BehaviorTracker _tracker;

	public override void _Ready()
	{
		_tracker = GetNode<BehaviorTracker>("/root/BehaviorTracker");
	}

	public void Interact(Node3D interactor)
	{
		SetOpen(!IsOpen);
		_tracker.ReportDoorInteraction(IsOpen);
	}

	public void SetOpen(bool open)
	{
		IsOpen = open;

		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(
			this,
			"rotation:y",
			Mathf.DegToRad(open ? OpenAngleDegrees : 0.0f),
			OpenTime
		).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	}
}
