public enum BehaviorContext
{
	None,

	DoorOpened,
	DoorClosed,

	ObjectInteracted,

	RoomEntered,
	RoomExited,
}

public enum BehaviorAction
{
	Idle,

	MoveForward,
	MoveBackward,
	MoveLeft,
	MoveRight,

	LookLeft,
	LookRight,

	TurnAround,
}

public readonly record struct Prediction(
	BehaviorAction Action,
	float Probability,
	float Confidence,
	int Samples
);

//результат одного behavioral observation.
public readonly record struct BehaviorObservationResult(
	BehaviorContext Context,
	BehaviorAction ActualAction,
	Prediction? Prediction,
	bool WasEvaluated,
	bool WasHit,
	double ReactionTime
);
