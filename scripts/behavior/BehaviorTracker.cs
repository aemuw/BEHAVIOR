using Godot;
using System;

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
	
	public BehaviorProfile Profile { get; } = new();
	
	public event Action<BehaviorObservationResult> ObservationCompleted;
	
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

	private RichTextLabel _movementDebug;
	private RichTextLabel _lookDebug;
	private RichTextLabel _spatialDebug;
	private RichTextLabel _interactionDebug;
	private RichTextLabel _profileDebug;
	private RichTextLabel _predictionDebug;
	
	public override void _Ready()
	{
		_debugLayer =
			new CanvasLayer
			{
				Visible = false
			};

		PanelContainer mainPanel =
			new PanelContainer
			{
				Position =
					new Vector2(18, 18),

				Size =
					new Vector2(960, 590)
			};

		_debugLayer.AddChild(
			mainPanel
		);

		MarginContainer margin =
			new MarginContainer();

		margin.AddThemeConstantOverride(
			"margin_left",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			10
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			10
		);

		mainPanel.AddChild(margin);

		VBoxContainer root =
			new VBoxContainer();

		margin.AddChild(root);

		HBoxContainer titleRow =
			new HBoxContainer();

		root.AddChild(titleRow);

		Label title =
			new Label
			{
				Text = "BEHAVIOR DEBUG"
			};

		title.AddThemeFontSizeOverride(
			"font_size",
			20
		);

		titleRow.AddChild(title);

		Control spacer =
			new Control
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		titleRow.AddChild(spacer);

		Label hint =
			new Label
			{
				Text = "F3 — toggle"
			};

		titleRow.AddChild(hint);

		root.AddChild(
			new HSeparator()
		);

		GridContainer grid =
			new GridContainer
			{
				Columns = 2,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};

		grid.AddThemeConstantOverride(
			"h_separation",
			10
		);

		grid.AddThemeConstantOverride(
			"v_separation",
			10
		);

		root.AddChild(grid);

		grid.AddChild(
			CreateDebugSection(
				"MOVEMENT",
				out _movementDebug
			)
		);

		grid.AddChild(
			CreateDebugSection(
				"LOOK",
				out _lookDebug
			)
		);

		grid.AddChild(
			CreateDebugSection(
				"SPATIAL",
				out _spatialDebug
			)
		);

		grid.AddChild(
			CreateDebugSection(
				"INTERACTION",
				out _interactionDebug
			)
		);

		grid.AddChild(
			CreateDebugSection(
				"BEHAVIOR PROFILE",
				out _profileDebug
			)
		);

		grid.AddChild(
			CreateDebugSection(
				"PREDICTION / MODEL",
				out _predictionDebug
			)
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

		Profile.Recalculate(
			Movement,
			Look,
			Spatial,
			Interactions,
			Model
		);

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

	public Door LastInteractedDoor { get; private set; }

	public void ReportDoorInteraction(
		bool opened,
		Door door)
	{
		LastInteractedDoor = door;

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

		_observationActionDetected = true;
		_observing = false;

		_lastObservedAction = action;

		LastReactionTime =
			Mathf.Clamp(
				(float)reactionTime,
				0.0f,
				(float)ObservationWindow
			);

		Prediction? prediction =
			_currentPrediction;

		bool wasEvaluated =
			prediction.HasValue &&
			prediction.Value.Samples >= 3;

		bool wasHit =
			wasEvaluated &&
			prediction.Value.Action == action;

		Model.Observe(
			_observationContext,
			action
		);

		ObservationCompleted?.Invoke(
			new BehaviorObservationResult(
				_observationContext,
				action,
				prediction,
				wasEvaluated,
				wasHit,
				LastReactionTime
			)
		);
	}

	private PanelContainer CreateDebugSection(
		string title,
		out RichTextLabel content)
	{
		PanelContainer panel =
			new PanelContainer
			{
				CustomMinimumSize =
					new Vector2(0, 145),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		VBoxContainer box =
			new VBoxContainer();

		box.AddThemeConstantOverride(
			"separation",
			4
		);

		panel.AddChild(box);

		Label header =
			new Label
			{
				Text = title
			};

		header.AddThemeFontSizeOverride(
			"font_size",
			16
		);

		box.AddChild(header);

		HSeparator separator =
			new HSeparator();

		box.AddChild(separator);

		ScrollContainer scroll =
			new ScrollContainer
			{
				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};

		box.AddChild(scroll);

		content =
			new RichTextLabel
			{
				BbcodeEnabled = true,

				FitContent = true,

				ScrollActive = false,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		content.AddThemeFontSizeOverride(
			"normal_font_size",
			14
		);

		scroll.AddChild(content);

		return panel;
	}

	private void UpdateDebugText()
	{
		_movementDebug.Text =
			$"[font_size=14]" +
			$"Time: {TotalTime:F1}s\n\n" +

			$"Walk:        {WalkTime:F1}s\n" +
			$"Run:         {RunTime:F1}s\n" +
			$"Stand still: {StandStillTime:F1}s\n" +
			$"Distance:    {DistanceWalked:F1} m\n" +
			$"Avg speed:   {Movement.AverageMovingSpeed:F2} m/s\n\n" +

			$"Forward:     {Movement.ForwardDistance:F1} m\n" +
			$"Backward:    {Movement.BackwardDistance:F1} m\n" +
			$"Left:        {Movement.LeftDistance:F1} m\n" +
			$"Right:       {Movement.RightDistance:F1} m\n" +
			$"Reversals:   {Movement.DirectionReversalCount}\n" +
			$"Sprint starts: {Movement.SprintStartCount}" +
			$"[/font]";

		_lookDebug.Text =
			$"[font_size=14]" +
			$"Yaw travel:   {Look.TotalYawTravel:F1} rad\n" +
			$"Pitch travel: {Look.TotalPitchTravel:F1} rad\n" +
			$"Large turns:  {Look.LargeTurnCount}\n\n" +

			$"Left travel:  {Look.LeftYawTravel:F1}\n" +
			$"Right travel: {Look.RightYawTravel:F1}" +
			$"[/font]";

		_spatialDebug.Text =
			$"[font_size=14]" +
			$"Unique cells:     {Spatial.UniqueCells}\n" +
			$"Revisited cells:  {Spatial.RevisitedCells}\n" +
			$"Backtrack events: {Spatial.BacktrackEvents}\n" +
			$"Backtrack dist:   {Spatial.BacktrackDistance:F1} m\n\n" +

			$"Novelty rate:     {Spatial.NoveltyRate:P0}\n" +
			$"Revisit rate:     {Spatial.RevisitRate:P0}" +
			$"[/font]";

		_interactionDebug.Text =
			$"[font_size=14]" +
			$"Interactions:        {InteractionCount}\n" +
			$"Doors:               {DoorInteractionCount}\n" +
			$"Unique seen:         {Interactions.UniqueTargetsSeen}\n" +
			$"Unique interacted:   {Interactions.UniqueTargetsInteracted}\n\n" +

			$"Focus events:        {Interactions.FocusEvents}\n" +
			$"Meaningful focus:    {Interactions.MeaningfulFocusEvents}\n" +
			$"Deep focus:          {Interactions.DeepFocusEvents}\n" +
			$"Ignored focus:       {Interactions.IgnoredFocusEvents}\n\n" +

			$"Avg focus:            {Interactions.AverageFocusTime:F2}s\n" +
			$"Max focus:            {Interactions.MaxFocusTime:F2}s\n" +
			$"Avg hesitation:       {AverageHesitation:F2}s\n" +
			$"Max hesitation:       {Interactions.MaxHesitation:F2}s" +
			$"[/font]";

		_profileDebug.Text =
			$"[font_size=14]" +

			$"EXPLORATION\n" +
			$"Value:      {Profile.Exploration.Value:P0}\n" +
			$"Confidence: {Profile.Exploration.Confidence:P0}\n\n" +

			$"CURIOSITY\n" +
			$"Value:      {Profile.Curiosity.Value:P0}\n" +
			$"Confidence: {Profile.Curiosity.Confidence:P0}\n\n" +

			$"AVOIDANCE\n" +
			$"Value:      {Profile.Avoidance.Value:P0}\n" +
			$"Confidence: {Profile.Avoidance.Confidence:P0}\n\n" +

			$"REPETITION\n" +
			$"Value:      {Profile.Repetition.Value:P0}\n" +
			$"Confidence: {Profile.Repetition.Confidence:P0}\n\n" +

			$"HESITATION\n" +
			$"Value:      {Profile.Hesitation.Value:P0}\n" +
			$"Confidence: {Profile.Hesitation.Confidence:P0}\n\n" +

			$"Overall confidence: " +
			$"{Profile.OverallConfidence:P0}" +

			$"[/font]";

		string prediction =
			_currentPrediction is Prediction p

				? $"Context:      {_observationContext}\n" +
				  $"Action:       {p.Action}\n" +
				  $"Probability:  {p.Probability:P0}\n" +
				  $"Confidence:   {p.Confidence:P0}\n" +
				  $"Samples:      {p.Samples}"

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
				? $"ACTIVE ({TotalTime - _observationStart:F2}s / {ObservationWindow:F1}s)"
				: "IDLE";

		_predictionDebug.Text =
			$"[font_size=14]" +
			$"Observation: {observation}\n\n" +

			$"{prediction}\n\n" +

			$"Observed:       {observed}\n" +
			$"Result:         {result}\n" +
			$"Reaction time:  {LastReactionTime:F2}s\n\n" +

			$"Predictability: {Model.Predictability:P0}\n" +
			$"Recent:         {Model.RecentPredictability:P0}\n" +
			$"Above chance:   {Model.PredictabilityAboveChance:P0}\n" +
			$"Brier:          {Model.LastBrierScore:F3}" +
			$"[/font]";
	}
}
