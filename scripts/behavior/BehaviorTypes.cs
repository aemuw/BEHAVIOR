//контекст: що відбулося перед прогнозом
public enum BehaviorContext
{
	None,

	DoorOpened,
	DoorClosed,

	ObjectInteracted,

	RoomEntered,
	RoomExited,
}

//перша помітна дія гравця після контексту
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

//результат прогнозу
//Probability = наскільки ймовірна конкретна дія
//Confidence = скільки даних ми маємо
//Samples = кількість попередніх спостережень контексту
public readonly record struct Prediction(
	BehaviorAction Action,
	float Probability,
	float Confidence,
	int Samples
);
