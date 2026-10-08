public abstract class BehaviorEvent
{
	public abstract string Id { get; }

	public virtual int Priority =>
		0;

	public abstract bool CanExecute(
		BehaviorObservationResult result,
		BehaviorTracker tracker
	);

	public abstract void Execute(
		BehaviorObservationResult result,
		BehaviorTracker tracker
	);
}
