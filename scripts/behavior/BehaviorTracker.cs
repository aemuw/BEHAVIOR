using System.Collections.Generic;
using Godot;

//autoload-синглтон: збирає дані про поведінку гравця.
//F3 вмикає/вимикає debug-панель (тільки для розробки).
public partial class BehaviorTracker : Node
{
	//розворот на цей кут (радіани, ~130°) за вікно в 1 секунду вважається "озирнувся назад".
	private const float LookBackAngle = 2.3f;
	private const double LookBackWindow = 1.0;
	private const double LookBackCooldown = 2.0;

	public double TotalTime { get; private set; }
	public double WalkTime { get; private set; }
	public double RunTime { get; private set; }
	public double StandStillTime { get; private set; }
	public float DistanceWalked { get; private set; }
	public int LookBackCount { get; private set; }

	private readonly Queue<(double Time, float Yaw)> _yawSamples = new();
	private float _yawSum;
	private double _lastLookBackTime = -100.0;

	private CanvasLayer _debugLayer;
	private Label _debugLabel;

	public override void _Ready()
	{
		_debugLayer = new CanvasLayer { Visible = false };
		_debugLabel = new Label { Position = new Vector2(12, 12) };
		_debugLayer.AddChild(_debugLabel);
		AddChild(_debugLayer);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F3 })
		{
			_debugLayer.Visible = !_debugLayer.Visible;
		}
	}

	public override void _Process(double delta)
	{
		TotalTime += delta;

		while (_yawSamples.Count > 0 && TotalTime - _yawSamples.Peek().Time > LookBackWindow)
		{
			_yawSum -= _yawSamples.Dequeue().Yaw;
		}

		if (Mathf.Abs(_yawSum) >= LookBackAngle && TotalTime - _lastLookBackTime > LookBackCooldown)
		{
			LookBackCount++;
			_lastLookBackTime = TotalTime;
		}

		if (_debugLayer.Visible)
		{
			_debugLabel.Text =
				$"Time: {TotalTime:F1}s\n" +
				$"Walk: {WalkTime:F1}s\n" +
				$"Run: {RunTime:F1}s\n" +
				$"Standing: {StandStillTime:F1}s\n" +
				$"Distance: {DistanceWalked:F1}m\n" +
				$"Look-backs: {LookBackCount}";
		}
	}

	//викликається гравцем при повороті вліво/вправо (радіани).
	public void ReportLook(float yawDelta)
	{
		_yawSamples.Enqueue((TotalTime, yawDelta));
		_yawSum += yawDelta;
	}

	//викликається гравцем щофізичний кадр.
	public void ReportMovement(double delta, float speed, bool sprinting)
	{
		if (speed < 0.1f)
		{
			StandStillTime += delta;
			return;
		}

		if (sprinting)
		{
			RunTime += delta;
		}
		else
		{
			WalkTime += delta;
		}

		DistanceWalked += speed * (float)delta;
	}
}
