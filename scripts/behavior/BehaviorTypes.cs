//контекст: що сталося (після чого спостерігаємо реакцію гравця)
public enum BehaviorContext
{
	DoorOpened,
	DoorClosed,
}

//наслідок: що гравець зробив у вікні спостереження після контексту
public enum BehaviorAction
{
	LookLeft,
	LookRight,
	MoveForward,
	MoveBack,
	Idle,
}

//прогноз моделі для контексту
//Probability - ймовірність найімовірнішої дії (0..1)
//Confidence - наскільки можна довіряти (росте з кількістю спостережень, 0..1)
//Samples - скільки разів цей контекст уже спостерігали
public readonly record struct Prediction(
	BehaviorAction Action,
	float Probability,
	float Confidence,
	int Samples
);
