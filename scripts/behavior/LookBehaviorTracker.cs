using Godot;

//відповідає за поведінку погляду та повороти
public sealed class LookBehaviorTracker
{
	private const float TurnAroundAngle = 2.3f; // ~132°
	private const double TurnBurstTimeout = 0.35;
	private const double TurnCooldown = 1.5;

	public float TotalYawTravel { get; private set; }
	public float TotalPitchTravel { get; private set; }

	public float LeftYawTravel { get; private set; }
	public float RightYawTravel { get; private set; }

	public int LargeTurnCount { get; private set; }

	public int LookBackCount => LargeTurnCount;

	private float _burstAngle;
	private int _burstDirection;

	private double _burstTimer;
	private double _cooldownTimer;

	public void Update(double delta)
	{
		_burstTimer += delta;
		_cooldownTimer += delta;

		if (_burstTimer > TurnBurstTimeout)
		{
			ResetBurst();
		}
	}

	public void ReportLook(
		float yawDelta,
		float pitchDelta)
	{
		TotalYawTravel += Mathf.Abs(yawDelta);
		TotalPitchTravel += Mathf.Abs(pitchDelta);

		if (yawDelta > 0.0f)
		{
			LeftYawTravel += yawDelta;
		}
		else if (yawDelta < 0.0f)
		{
			RightYawTravel += Mathf.Abs(yawDelta);
		}

		if (Mathf.Abs(yawDelta) < 0.0005f)
		{
			return;
		}

		int direction =
			yawDelta > 0.0f ? 1 : -1;

		if (_burstDirection == 0 ||
			direction != _burstDirection ||
			_burstTimer > TurnBurstTimeout)
		{
			_burstDirection = direction;
			_burstAngle = Mathf.Abs(yawDelta);
		}
		else
		{
			_burstAngle += Mathf.Abs(yawDelta);
		}

		_burstTimer = 0.0;

		if (_burstAngle >= TurnAroundAngle &&
			_cooldownTimer >= TurnCooldown)
		{
			LargeTurnCount++;

			_cooldownTimer = 0.0;

			ResetBurst();
		}
	}

	public float GetYawTravelPerSecond(
		double totalTime)
	{
		return totalTime > 0.0001
			? TotalYawTravel / (float)totalTime
			: 0.0f;
	}

	private void ResetBurst()
	{
		_burstAngle = 0.0f;
		_burstDirection = 0;
		_burstTimer = 0.0;
	}
}
