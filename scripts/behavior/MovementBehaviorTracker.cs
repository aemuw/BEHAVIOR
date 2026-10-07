using Godot;

//відповідає тільки за рухові метрики
public sealed class MovementBehaviorTracker
{
	private const float MovementThreshold = 0.1f;
	private const float DirectionThreshold = 0.2f;
	private const double DirectionChangeCooldown = 0.4;

	public double WalkTime { get; private set; }
	public double RunTime { get; private set; }
	public double StandStillTime { get; private set; }

	public float DistanceWalked { get; private set; }
	public float ForwardDistance { get; private set; }
	public float BackwardDistance { get; private set; }
	public float LeftDistance { get; private set; }
	public float RightDistance { get; private set; }

	public int DirectionReversalCount { get; private set; }
	public int SprintStartCount { get; private set; }
	public int StopCount { get; private set; }

	public float AverageMovingSpeed
	{
		get
		{
			double movingTime = WalkTime + RunTime;

			return movingTime > 0.0001
				? DistanceWalked / (float)movingTime
				: 0.0f;
		}
	}

	private int _lastForwardSign;
	private int _lastLateralSign;

	private double _directionChangeTimer = 999.0;

	private bool _wasMoving;
	private bool _wasSprinting;

	public void Update(
		double delta,
		float speed,
		bool sprinting,
		float forwardSpeed,
		float lateralSpeed)
	{
		double safeDelta = Mathf.Max((float)delta, 0.0f);

		if (speed < MovementThreshold)
		{
			StandStillTime += safeDelta;

			if (_wasMoving)
			{
				StopCount++;
			}

			_wasMoving = false;
			_wasSprinting = false;

			_lastForwardSign = 0;
			_lastLateralSign = 0;

			_directionChangeTimer += safeDelta;

			return;
		}

		_wasMoving = true;
		_directionChangeTimer += safeDelta;

		DistanceWalked += speed * (float)safeDelta;

		if (sprinting)
		{
			RunTime += safeDelta;

			if (!_wasSprinting)
			{
				SprintStartCount++;
			}
		}
		else
		{
			WalkTime += safeDelta;
		}

		_wasSprinting = sprinting;

		float forwardDistance =
			forwardSpeed * (float)safeDelta;

		float lateralDistance =
			lateralSpeed * (float)safeDelta;

		if (forwardDistance > 0.0f)
		{
			ForwardDistance += forwardDistance;
		}
		else
		{
			BackwardDistance += Mathf.Abs(forwardDistance);
		}

		if (lateralDistance > 0.0f)
		{
			RightDistance += lateralDistance;
		}
		else if (lateralDistance < 0.0f)
		{
			LeftDistance += Mathf.Abs(lateralDistance);
		}

		int forwardSign =
			SignWithDeadzone(forwardSpeed, DirectionThreshold);

		int lateralSign =
			SignWithDeadzone(lateralSpeed, DirectionThreshold);

		if (_directionChangeTimer >= DirectionChangeCooldown)
		{
			if (forwardSign != 0 &&
				_lastForwardSign != 0 &&
				forwardSign != _lastForwardSign)
			{
				DirectionReversalCount++;
				_directionChangeTimer = 0.0;
			}
			else if (lateralSign != 0 &&
					 _lastLateralSign != 0 &&
					 lateralSign != _lastLateralSign)
			{
				DirectionReversalCount++;
				_directionChangeTimer = 0.0;
			}
		}

		if (forwardSign != 0)
		{
			_lastForwardSign = forwardSign;
		}

		if (lateralSign != 0)
		{
			_lastLateralSign = lateralSign;
		}
	}

	private static int SignWithDeadzone(
		float value,
		float deadzone)
	{
		if (value > deadzone)
		{
			return 1;
		}

		if (value < -deadzone)
		{
			return -1;
		}

		return 0;
	}
}
