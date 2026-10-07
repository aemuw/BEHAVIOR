using Godot;

//усе, з чим можна взаємодіяти клавішею E, реалізує цей інтерфейс
public interface IInteractable
{
	void Interact(Node3D interactor);
}
