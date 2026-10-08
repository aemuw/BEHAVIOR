using System.Collections.Generic;
using System.Linq;
using Godot;

//відстежує, в яких кімнатах буває гравець:
//входи, повернення, час у темних і світлих кімнатах
public sealed class RoomBehaviorTracker
{
	//повторний вхід в ту саму кімнату швидше ніж за цей час
	//вважаємо дрижанням на порозі, а не справжнім входом
	private const double ReentryJitterTime = 1.0;

	public sealed class RoomStats
	{
		public string Id { get; init; } = "";
		public bool IsDark { get; set; }
		public int Entries { get; set; }
		public double TimeInside { get; set; }
		public double LastExitTime { get; set; } = -999.0;
	}

	//усі кімнати рівня (реєструються самі при старті)
	private readonly Dictionary<string, bool> _registered = new();

	//кімнати, які гравець відвідав
	private readonly Dictionary<string, RoomStats> _rooms = new();

	//зони, в яких гравець перебуває просто зараз
	//(на порозі капсула може торкатися двох зон одразу)
	private readonly List<string> _active = new();

	public string CurrentRoomId { get; private set; } = "";

	public string PreviousRoomId { get; private set; } = "";

	public string CurrentRoomLabel =>
		CurrentRoomId.Length == 0
			? "-"
			: CurrentRoomId;

	public int RoomEntries { get; private set; }
	public int DarkEntries { get; private set; }
	public int LitEntries { get; private set; }
	public int ReturnVisits { get; private set; }

	public double DarkTime { get; private set; }
	public double LitTime { get; private set; }

	public int RegisteredRooms =>
		_registered.Count;

	public int RegisteredDarkRooms =>
		_registered.Values.Count(dark => dark);

	public int UniqueRoomsVisited =>
		_rooms.Count;

	public IReadOnlyDictionary<string, RoomStats> Visited =>
		_rooms;

	//різниця між тим, наскільки гравець дослідив світлі й темні кімнати
	//0 = темні й світлі відвідувані однаково, 1 = темні повністю оминає
	public float DarkAvoidance
	{
		get
		{
			int totalDark = RegisteredDarkRooms;
			int totalLit = RegisteredRooms - totalDark;

			if (totalDark == 0 || totalLit == 0)
			{
				return 0.0f;
			}

			int visitedDark =
				_rooms.Values.Count(room => room.IsDark);

			int visitedLit =
				_rooms.Count - visitedDark;

			float darkCoverage =
				(float)visitedDark / totalDark;

			float litCoverage =
				(float)visitedLit / totalLit;

			return Mathf.Clamp(
				litCoverage - darkCoverage,
				0.0f,
				1.0f
			);
		}
	}

	public RoomStats GetStats(string roomId)
	{
		return _rooms.TryGetValue(roomId, out RoomStats stats)
			? stats
			: null;
	}

	public void RegisterRoom(string roomId, bool isDark)
	{
		_registered[roomId] = isDark;
	}

	//true, якщо це справжній вхід у нову поточну кімнату
	public bool ReportEnter(
		string roomId,
		bool isDark,
		double time)
	{
		if (_active.Contains(roomId))
		{
			return false;
		}

		_active.Add(roomId);

		string previous = CurrentRoomId;
		CurrentRoomId = roomId;

		bool firstVisit = false;

		if (!_rooms.TryGetValue(roomId, out RoomStats stats))
		{
			stats = new RoomStats { Id = roomId };
			_rooms[roomId] = stats;
			firstVisit = true;
		}

		stats.IsDark = isDark;

		if (previous.Length > 0 && previous != roomId)
		{
			PreviousRoomId = previous;
		}

		bool jitter =
			!firstVisit &&
			time - stats.LastExitTime < ReentryJitterTime;

		if (jitter)
		{
			return false;
		}

		stats.Entries++;
		RoomEntries++;

		if (isDark)
		{
			DarkEntries++;
		}
		else
		{
			LitEntries++;
		}

		if (!firstVisit)
		{
			ReturnVisits++;
		}

		return true;
	}

	public void ReportExit(
		string roomId,
		double time)
	{
		if (!_active.Remove(roomId))
		{
			return;
		}

		if (_rooms.TryGetValue(roomId, out RoomStats stats))
		{
			stats.LastExitTime = time;
		}

		if (CurrentRoomId != roomId)
		{
			return;
		}

		PreviousRoomId = roomId;

		CurrentRoomId =
			_active.Count > 0
				? _active[_active.Count - 1]
				: "";
	}

	public void Update(double delta)
	{
		if (CurrentRoomId.Length == 0)
		{
			return;
		}

		if (!_rooms.TryGetValue(CurrentRoomId, out RoomStats stats))
		{
			return;
		}

		stats.TimeInside += delta;

		if (stats.IsDark)
		{
			DarkTime += delta;
		}
		else
		{
			LitTime += delta;
		}
	}
}
