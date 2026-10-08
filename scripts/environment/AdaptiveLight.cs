using Godot;

public partial class AdaptiveLight : OmniLight3D
{
	[Export]
	public float FlickerMinEnergy { get; set; } = 0.03f;

	[Export]
	public float FlickerDuration { get; set; } = 0.75f;

	[Export]
	public int FlickerPulses { get; set; } = 3;

	private float _baseEnergy;
	private bool _isFlickering;

	public override void _Ready()
	{
		_baseEnergy = LightEnergy;
	}

	public bool IsFlickering =>
		_isFlickering;

	public void TriggerFlicker()
	{
		if (_isFlickering)
		{
			return;
		}

		_isFlickering = true;

		float pulseDuration =
			FlickerDuration /
			(FlickerPulses * 2.0f);

		Tween tween =
			CreateTween();

		for (int i = 0;
			 i < FlickerPulses;
			 i++)
		{
			tween.TweenProperty(
				this,
				"light_energy",
				FlickerMinEnergy,
				pulseDuration
			);

			tween.TweenProperty(
				this,
				"light_energy",
				_baseEnergy,
				pulseDuration
			);
		}

		tween.Finished += () =>
		{
			LightEnergy =
				_baseEnergy;

			_isFlickering = false;
		};
	}
}
