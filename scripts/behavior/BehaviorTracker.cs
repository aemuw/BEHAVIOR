using Godot;

public partial class BehaviorTracker : Node
{
	//максимально довго чекаємо на першу реакцію після контексту
	private const double ObservationWindow = 1.5;

	private const float ObservationMoveThreshold = 0.20f;
	private const float ObservationLookThreshold = 0.45f;
	private const float ObservationTurnAroundThreshold = 2.3f;

	public double TotalTime { get; private set; }

	public MovementBehaviorTracker Movement { get; } = new();
	public LookBehaviorTracker Look { get; } = new();
	public InteractionBehaviorTracker Interactions { get; } = new();

	public BehaviorModel Model { get; } = new();

	public double WalkTime =>
		Movement.WalkTime;

	public double RunTime =>
		Movement.RunTime;

	public double StandStillTime =>
		Movement.StandStillTime;

	public float DistanceWalked =>
		Movement.DistanceWalked;

	public int LookBackCount =>
		Look.LookBackCount;

	public int InteractionCount =>
		Interactions.InteractionCount;

	public int DoorInteractionCount =>
		Interactions.DoorInteractionCount;

	public double LastHesitation =>
		Interactions.LastHesitation;

	public double AverageHesitation =>
		Interactions.AverageHesitation;

	public bool IsObserving =>
		_observing;

	public BehaviorContext ObservationContext =>
		_observationContext;

	public Prediction? CurrentPrediction =>
		_currentPrediction;

	public BehaviorAction? LastObservedAction =>
		_lastObservedAction;

	public double LastReactionTime { get; private set; }

	private bool _observing;

	private BehaviorContext _observationContext;

	private Prediction? _currentPrediction;

	private BehaviorAction? _lastObservedAction;

	private double _observationStart;

	private float _observationYaw;

	private float _observationForward;
	private float _observationBackward;

	private float _observationLeft;
	private float _observationRight;

	private bool _observationActionDetected;

	private CanvasLayer _debugLayer;
	private Label _debugLabel;

	public override void _Ready()
	{
		_debugLayer =
			new CanvasLayer
			{
				Visible = false
			};

		_debugLabel =
			new Label
			{
				Position = new Vector2(
					12,
					12
				),
				Size = new Vector2(
					620,
					500
				)
			};

		_debugLayer.AddChild(
			_debugLabel
		);

		AddChild(
			_debugLayer
		);
	}

	public override void _UnhandledInput(
		InputEvent @event)
	{
		if (@event is InputEventKey
			{
				Pressed: true,
				Echo: false,
				Keycode: Key.F3
			})
		{
			_debugLayer.Visible =
				!_debugLayer.Visible;
		}
	}

	public override void _Process(
		double delta)
	{
		TotalTime += delta;

		Look.Update(delta);

		if (_observing &&
			!_observationActionDetected &&
			TotalTime -
			_observationStart >=
			ObservationWindow)
		{
			CompleteObservation(
				BehaviorAction.Idle,
				ObservationWindow
			);
		}

		if (_debugLayer.Visible)
		{
			UpdateDebugText();
		}
	}

	//викликається PlayerController
	public void ReportLook(
		float yawDelta,
		float pitchDelta)
	{
		Look.ReportLook(
			yawDelta,
			pitchDelta
		);

		if (!_observing ||
			_observationActionDetected)
		{
			return;
		}

		_observationYaw +=
			yawDelta;

		if (Mathf.Abs(
				_observationYaw)
			>= ObservationTurnAroundThreshold)
		{
			CompleteObservation(
				BehaviorAction.TurnAround,
				TotalTime -
				_observationStart
			);

			return;
		}

		if (Mathf.Abs(
				_observationYaw)
			>= ObservationLookThreshold)
		{
			CompleteObservation(
				_observationYaw > 0.0f
					? BehaviorAction.LookLeft
					: BehaviorAction.LookRight,

				TotalTime -
				_observationStart
			);
		}
	}

	public void ReportMovement(
		double delta,
		float speed,
		bool sprinting,
		float forwardSpeed,
		float lateralSpeed)
	{
		Movement.Update(
			delta,
			speed,
			sprinting,
			forwardSpeed,
			lateralSpeed
		);

		if (!_observing ||
			_observationActionDetected ||
			speed < ObservationMoveThreshold)
		{
			return;
		}

		_observationForward +=
			Mathf.Max(
				forwardSpeed,
				0.0f
			) *
			(float)delta;

		_observationBackward +=
			Mathf.Max(
				-forwardSpeed,
				0.0f
			) *
			(float)delta;

		_observationRight +=
			Mathf.Max(
				lateralSpeed,
				0.0f
			) *
			(float)delta;

		_observationLeft +=
			Mathf.Max(
				-lateralSpeed,
				0.0f
			) *
			(float)delta;

		float[] values =
		{
			_observationForward,
			_observationBackward,
			_observationLeft,
			_observationRight
		};

		int maxIndex = 0;

		for (int i = 1;
			 i < values.Length;
			 i++)
		{
			if (values[i] >
				values[maxIndex])
			{
				maxIndex = i;
			}
		}

		if (values[maxIndex] < 0.08f)
		{
			return;
		}

		BehaviorAction action =
			maxIndex switch
			{
				0 =>
					BehaviorAction.MoveForward,

				1 =>
					BehaviorAction.MoveBackward,

				2 =>
					BehaviorAction.MoveLeft,

				_ =>
					BehaviorAction.MoveRight
			};

		CompleteObservation(
			action,
			TotalTime -
			_observationStart
		);
	}

	public void ReportInteraction(
		double hesitation)
	{
		Interactions.ReportInteraction(
			hesitation
		);
	}

	public void ReportDoorInteraction(
		bool opened)
	{
		Interactions.ReportDoorInteraction();

		BeginObservation(
			opened
				? BehaviorContext.DoorOpened
				: BehaviorContext.DoorClosed
		);
	}

	public void BeginObservation(
		BehaviorContext context)
	{
		if (context ==
			BehaviorContext.None)
		{
			return;
		}

		if (_observing &&
			!_observationActionDetected)
		{
			CompleteObservation(
				BehaviorAction.Idle,
				TotalTime -
				_observationStart
			);
		}

		_observationContext =
			context;

		_currentPrediction =
			Model.Predict(context);

		_observationStart =
			TotalTime;

		_observationYaw = 0.0f;

		_observationForward = 0.0f;
		_observationBackward = 0.0f;

		_observationLeft = 0.0f;
		_observationRight = 0.0f;

		_observationActionDetected =
			false;

		_observing = true;
	}

	private void CompleteObservation(
		BehaviorAction action,
		double reactionTime)
	{
		if (!_observing ||
			_observationActionDetected)
		{
			return;
		}

		_observationActionDetected =
			true;

		_observing = false;

		_lastObservedAction =
			action;

		LastReactionTime =
			Mathf.Clamp(
				(float)reactionTime,
				0.0f,
				(float)ObservationWindow
			);

		Model.Observe(
			_observationContext,
			action
		);
	}

	private void UpdateDebugText()
	{
		string prediction =
			_currentPrediction is Prediction p

				? $"{_observationContext} -> {p.Action} | " +
				  $"P={p.Probability:P0} | " +
				  $"C={p.Confidence:P0} | " +
				  $"N={p.Samples}"

				: "-";

		string observed =
			_lastObservedAction?.ToString()
			?? "-";

		string result =
			Model.HasLastEvaluation

				? Model.LastWasHit
					? "HIT"
					: "MISS"

				: "-";

		string observation =
			_observing

				? $"OBSERVING " +
				  $"({TotalTime - _observationStart:F2}s / " +
				  $"{ObservationWindow:F1}s)"

				: "idle";

		_debugLabel.Text =
			$"BEHAVIOR DEBUG\n\n" +

			$"Time: {TotalTime:F1}s\n" +

			$"Walk: {WalkTime:F1}s   " +
			$"Run: {RunTime:F1}s   " +
			$"Stand: {StandStillTime:F1}s\n" +

			$"Distance: {DistanceWalked:F1}m   " +
			$"Avg speed: " +
			$"{Movement.AverageMovingSpeed:F2} m/s\n" +

			$"Forward: {Movement.ForwardDistance:F1}m   " +
			$"Back: {Movement.BackwardDistance:F1}m\n" +

			$"Left: {Movement.LeftDistance:F1}m   " +
			$"Right: {Movement.RightDistance:F1}m\n" +

			$"Direction reversals: " +
			$"{Movement.DirectionReversalCount}\n" +

			$"Sprint starts: " +
			$"{Movement.SprintStartCount}\n\n" +

			$"Yaw travel: " +
			$"{Look.TotalYawTravel:F1} rad   " +

			$"Pitch travel: " +
			$"{Look.TotalPitchTravel:F1} rad\n" +

			$"Large turns: " +
			$"{Look.LargeTurnCount}\n\n" +

			$"Interactions: " +
			$"{InteractionCount}   " +

			$"Doors: " +
			$"{DoorInteractionCount}\n" +

			$"Hesitation: last " +
			$"{LastHesitation:F2}s | " +

			$"avg " +
			$"{AverageHesitation:F2}s | " +

			$"max " +
			$"{Interactions.MaxHesitation:F2}s\n" +

			$"Quick: " +
			$"{Interactions.QuickInteractionCount}   " +

			$"Long: " +
			$"{Interactions.LongHesitationCount}\n\n" +

			$"Observation: " +
			$"{observation}\n" +

			$"Prediction: " +
			$"{prediction}\n" +

			$"Observed: " +
			$"{observed}   " +

			$"Result: " +
			$"{result}\n" +

			$"Reaction: " +
			$"{LastReactionTime:F2}s\n" +

			$"Predictability: " +
			$"{Model.Predictability:P0} " +
			$"({Model.Hits}/" +
			$"{Model.Evaluations})\n" +

			$"Recent: " +
			$"{Model.RecentPredictability:P0}\n" +

			$"Above chance: " +
			$"{Model.PredictabilityAboveChance:P0}\n" +

			$"Brier: " +
			$"{Model.LastBrierScore:F3}";
	}
}
