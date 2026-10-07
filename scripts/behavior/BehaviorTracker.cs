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

	public SpatialBehaviorTracker Spatial { get; } = new();

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

	public float ExplorationTendency =>
		Spatial.ExplorationTendency;

	public float BacktrackingTendency =>
		Spatial.BacktrackingTendency;
	
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

	private Label _leftDebugLabel;
	private Label _rightDebugLabel;

	public override void _Ready()
	{
		_debugLayer = new CanvasLayer
		{
			Visible = false
		};

		PanelContainer panel = new PanelContainer
		{
			Position = new Vector2(18, 18),
			Size = new Vector2(920, 565)
		};

		_debugLayer.AddChild(panel);

		MarginContainer margin = new MarginContainer();

		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			12
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			12
		);

		panel.AddChild(margin);

		VBoxContainer root = new VBoxContainer();

		margin.AddChild(root);

		Label title = new Label
		{
			Text = "BEHAVIOR DEBUG"
		};

		title.AddThemeFontSizeOverride(
			"font_size",
			20
		);

		root.AddChild(title);

		HSeparator separator = new HSeparator();

		root.AddChild(separator);

		HBoxContainer columns = new HBoxContainer
		{
			SizeFlagsVertical =
				Control.SizeFlags.ExpandFill
		};

		columns.AddThemeConstantOverride(
			"separation",
			30
		);

		root.AddChild(columns);

		_leftDebugLabel = new Label
		{
			SizeFlagsHorizontal =
				Control.SizeFlags.ExpandFill,

			AutowrapMode =
				TextServer.AutowrapMode.WordSmart
		};

		_rightDebugLabel = new Label
		{
			SizeFlagsHorizontal =
				Control.SizeFlags.ExpandFill,

			AutowrapMode =
				TextServer.AutowrapMode.WordSmart
		};

		_leftDebugLabel.AddThemeFontSizeOverride(
			"font_size",
			15
		);

		_rightDebugLabel.AddThemeFontSizeOverride(
			"font_size",
			15
		);

		columns.AddChild(
			_leftDebugLabel
		);

		columns.AddChild(
			_rightDebugLabel
		);

		AddChild(_debugLayer);
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
		float lateralSpeed,
		Vector3 worldPosition)
	{
		Spatial.Update(worldPosition);

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

				? $"{_observationContext}\n" +
				  $"Action: {p.Action}\n" +
				  $"Probability: {p.Probability:P0}\n" +
				  $"Confidence: {p.Confidence:P0}\n" +
				  $"Samples: {p.Samples}"

				: "No active prediction";

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

				? $"ACTIVE  " +
				  $"{TotalTime - _observationStart:F2}s / " +
				  $"{ObservationWindow:F1}s"

				: "IDLE";

		_leftDebugLabel.Text =
			"TIME\n" +
			$"Session: {TotalTime:F1}s\n\n" +

			"MOVEMENT\n" +
			$"Walk time:        {WalkTime:F1}s\n" +
			$"Run time:         {RunTime:F1}s\n" +
			$"Stand still:      {StandStillTime:F1}s\n" +
			$"Distance:         {DistanceWalked:F1} m\n" +
			$"Average speed:    {Movement.AverageMovingSpeed:F2} m/s\n\n" +

			"DIRECTIONAL MOVEMENT\n" +
			$"Forward:          {Movement.ForwardDistance:F1} m\n" +
			$"Backward:         {Movement.BackwardDistance:F1} m\n" +
			$"Left:             {Movement.LeftDistance:F1} m\n" +
			$"Right:            {Movement.RightDistance:F1} m\n" +
			$"Direction flips:  {Movement.DirectionReversalCount}\n" +
			$"Sprint starts:    {Movement.SprintStartCount}\n\n" +

			"SPATIAL\n" +
			$"Unique cells:     {Spatial.UniqueCells}\n" +
			$"Revisited:        {Spatial.RevisitedCells}\n" +
			$"Backtrack events: {Spatial.BacktrackEvents}\n" +
			$"Backtrack dist:   {Spatial.BacktrackDistance:F1} m\n" +
			$"Novelty rate:     {Spatial.NoveltyRate:P0}\n" +
			$"Revisit rate:     {Spatial.RevisitRate:P0}\n";

		_rightDebugLabel.Text =
			"LOOK\n" +
			$"Yaw travel:       {Look.TotalYawTravel:F1} rad\n" +
			$"Pitch travel:     {Look.TotalPitchTravel:F1} rad\n" +
			$"Large turns:      {Look.LargeTurnCount}\n\n" +

			"INTERACTION\n" +
			$"Interactions:        {InteractionCount}\n" +
			$"Doors:               {DoorInteractionCount}\n" +
			$"Unique seen:         {Interactions.UniqueTargetsSeen}\n" +
			$"Unique interacted:   {Interactions.UniqueTargetsInteracted}\n" +
			$"Focus events:        {Interactions.FocusEvents}\n" +
			$"Meaningful focus:    {Interactions.MeaningfulFocusEvents}\n" +
			$"Deep focus:          {Interactions.DeepFocusEvents}\n" +
			$"Ignored focus:       {Interactions.IgnoredFocusEvents}\n" +
			$"Focus time total:    {Interactions.TotalFocusTime:F1}s\n" +
			$"Focus time avg:      {Interactions.AverageFocusTime:F2}s\n" +
			$"Focus time max:      {Interactions.MaxFocusTime:F2}s\n" +
			$"Meaningful rate:     {Interactions.MeaningfulFocusRate:P0}\n" +
			$"Ignore rate:         {Interactions.DeliberateIgnoreRate:P0}\n" +
			$"Deep focus rate:     {Interactions.DeepFocusRate:P0}\n" +
			$"Last hesitation:     {LastHesitation:F2}s\n" +
			$"Average hesitation:  {AverageHesitation:F2}s\n" +
			$"Max hesitation:      {Interactions.MaxHesitation:F2}s\n" +
			$"Quick interactions:  {Interactions.QuickInteractionCount}\n" +
			$"Long hesitation:     {Interactions.LongHesitationCount}\n\n" +

			"BEHAVIOR PROFILE\n" +
			$"Exploration:      {ExplorationTendency:P0}\n" +
			$"Backtracking:     {BacktrackingTendency:P0}\n\n" +

			"PREDICTION\n" +
			$"{prediction}\n\n" +
			$"Observed:         {observed}\n" +
			$"Result:           {result}\n" +
			$"Reaction time:    {LastReactionTime:F2}s\n\n" +

			"MODEL\n" +
			$"Predictability:   {Model.Predictability:P0}\n" +
			$"Recent accuracy:  {Model.RecentPredictability:P0}\n" +
			$"Above chance:     {Model.PredictabilityAboveChance:P0}\n" +
			$"Brier score:      {Model.LastBrierScore:F3}";
	}
}
