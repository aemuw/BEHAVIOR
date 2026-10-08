public abstract class BehaviorEvent
{
	public abstract string Id { get; }

	public virtual int Priority =>
		0;

	//чи потрібен влучний прогноз моделі (серія влучань), щоб подія могла спрацювати
	public virtual bool RequiresPredictionHit =>
		true;

	//імовірність спрацювання, коли всі умови виконані.
	//Менше 1.0, щоб гра не реагувала щоразу і її не можна було вирахувати
	public virtual float Chance =>
		1.0f;

	//мінімальний час (секунди) між запусками саме цієї події
	public virtual double Cooldown =>
		0.0;

	public abstract bool CanExecute(
		BehaviorObservationResult result,
		BehaviorTracker tracker
	);

	public abstract void Execute(
		BehaviorObservationResult result,
		BehaviorTracker tracker
	);
}
