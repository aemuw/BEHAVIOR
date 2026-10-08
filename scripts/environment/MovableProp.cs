using Godot;

//предмет, який гра може непомітно зрушити з місця,
//поки гравець не дивиться
public partial class MovableProp : StaticBody3D
{
	public const string GroupName = "behavior_movable_prop";

	[Export]
	public string RoomId { get; set; } = "";

	//на скільки метрів предмет зсувається за один раз
	[Export]
	public float ShiftDistance { get; set; } = 0.35f;

	[Export]
	public float ShiftAngleDegrees { get; set; } = 30.0f;

	//скільки разів максимум можна зрушити предмет за гру
	[Export]
	public int MaxShifts { get; set; } = 3;

	[Export]
	public double MinSecondsBetweenShifts { get; set; } = 60.0;

	public int ShiftCount { get; private set; }

	private Vector3 _homePosition;

	private double _lastShiftTime = -9999.0;

	private static double Now =>
		Time.GetTicksMsec() / 1000.0;

	public bool CanShift =>
		ShiftCount < MaxShifts &&
		Now - _lastShiftTime >= MinSecondsBetweenShifts;

	public override void _Ready()
	{
		_homePosition = Position;

		AddToGroup(GroupName);
	}

	public void Shift()
	{
		ShiftCount++;

		_lastShiftTime = Now;

		//кожен зсув в іншому напрямку, але недалеко від початкового місця
		float angle = ShiftCount * 2.4f;

		Vector3 offset =
			new Vector3(
				Mathf.Cos(angle),
				0.0f,
				Mathf.Sin(angle)
			) *
			ShiftDistance *
			Mathf.Min(ShiftCount, 2);

		Position = _homePosition + offset;

		RotateY(Mathf.DegToRad(ShiftAngleDegrees));
	}
}
