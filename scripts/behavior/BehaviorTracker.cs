using System.Collections.Generic;
using Godot;

//збирає дані про поведінку гравця і веде модель передбачення
//F3 вмикає/вимикає debug-панель (тільки для розробки)
public partial class BehaviorTracker : Node
{
	//розворот на цей кут (радіани, +-130 градусів) за вікно в 1 секунду вважається "озирнувся назад"
	private const float LookBackAngle = 2.3f;
	private const double LookBackWindow = 1.0;
	private const double LookBackCooldown = 2.0;

	//скільки секунд після контексту спостерігаємо реакцію гравця
	private const double ObservationWindow = 2.5;
	private const float LookThreshold = 0.6f;   //радіани (+-35 градусів)
	private const float MoveThreshold = 1.0f;   //метри

	public double TotalTime { get; private set; }
	public double WalkTime { get; private set; }
	public double RunTime { get; private set; }
	public double StandStillTime { get; private set; }
	public float DistanceWalked { get; private set; }
	public int LookBackCount { get; private set; }

	public int InteractionCount { get; private set; }
	public int DoorInteractionCount { get; private set; }
	public double LastHesitation { get; private set; }
	public double AverageHesitation => InteractionCount > 0 ? _hesitationSum / InteractionCount : 0.0;

	public BehaviorModel Model { get; } = new();

	private double _hesitationSum;

	private readonly Queue<(double Time, float Yaw)> _yawSamples = new();
	private float _yawSum;
	private double _lastLookBackTime = -100.0;

	//поточне вікно спостереження
	private bool _observing;
	private BehaviorContext _obsContext;
	private double _obsEnd;
	private float _obsYaw;
	private float _obsForward;

	private Prediction? _lastPrediction;
	private BehaviorContext _lastContext;
	private BehaviorAction? _lastObserved;

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

		if (_observing && TotalTime >= _obsEnd)
		{
			FinishObservation();
		}

		if (_debugLayer.Visible)
		{
			UpdateDebugText();
		}
	}

	//повороти вліво/вправо (радіани; додатне = вліво)
	//викликає PlayerController
	public void ReportLook(float yawDelta)
	{
		_yawSamples.Enqueue((TotalTime, yawDelta));
		_yawSum += yawDelta;

		if (_observing)
		{
			_obsYaw += yawDelta;
		}
	}

	//рух за фізичний кадр
	//forwardSpeed: додатне = вперед відносно погляду
	public void ReportMovement(double delta, float speed, bool sprinting, float forwardSpeed)
	{
		if (_observing)
		{
			_obsForward += forwardSpeed * (float)delta;
		}

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

	//будь-яка взаємодія по E
	//hesitation = скільки секунд гравець дивився на об'єкт до натискання
	public void ReportInteraction(double hesitation)
	{
		InteractionCount++;
		LastHesitation = hesitation;
		_hesitationSum += hesitation;
	}

	public void ReportDoorInteraction(bool opened)
	{
		DoorInteractionCount++;
		BeginObservation(opened ? BehaviorContext.DoorOpened : BehaviorContext.DoorClosed);
	}

	//починає вікно спостереження: що гравець зробить далі?
	public void BeginObservation(BehaviorContext context)
	{
		_lastContext = context;
		_lastPrediction = Model.Predict(context);

		_observing = true;
		_obsContext = context;
		_obsEnd = TotalTime + ObservationWindow;
		_obsYaw = 0.0f;
		_obsForward = 0.0f;
	}

	private void FinishObservation()
	{
		_observing = false;
		BehaviorAction action = Classify();
		_lastObserved = action;
		Model.Observe(_obsContext, action);
	}

	private BehaviorAction Classify()
	{
		float lookScore = Mathf.Abs(_obsYaw) / LookThreshold;
		float moveScore = Mathf.Abs(_obsForward) / MoveThreshold;

		if (lookScore < 1.0f && moveScore < 1.0f)
		{
			return BehaviorAction.Idle;
		}

		if (lookScore >= moveScore)
		{
			return _obsYaw > 0.0f ? BehaviorAction.LookLeft : BehaviorAction.LookRight;
		}

		return _obsForward > 0.0f ? BehaviorAction.MoveForward : BehaviorAction.MoveBack;
	}

	private void UpdateDebugText()
	{
		string prediction = _lastPrediction is Prediction p
			? $"{_lastContext} -> {p.Action} {p.Probability:P0} (conf {p.Confidence:F2}, n={p.Samples})"
			: "-";
		string observed = _lastObserved?.ToString() ?? "-";
		string result = Model.HasLastEvaluation ? (Model.LastWasHit ? "HIT" : "MISS") : "-";

		_debugLabel.Text =
			$"Time: {TotalTime:F1}s\n" +
			$"Walk: {WalkTime:F1}s  Run: {RunTime:F1}s  Standing: {StandStillTime:F1}s\n" +
			$"Distance: {DistanceWalked:F1}m\n" +
			$"Look-backs: {LookBackCount}\n" +
			$"Interactions: {InteractionCount} (doors: {DoorInteractionCount})\n" +
			$"Hesitation last/avg: {LastHesitation:F2}s / {AverageHesitation:F2}s\n" +
			$"\n" +
			$"Prediction: {prediction}\n" +
			$"Observed: {observed}  [{result}]\n" +
			$"Predictability: {Model.Predictability:P0} ({Model.Hits}/{Model.Evaluations})\n" +
			$"Above chance: {Model.PredictabilityAboveChance:P0}";
	}
}
