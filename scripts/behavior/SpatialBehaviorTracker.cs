using System.Collections.Generic;
using Godot;

//відстежує просторову поведінку гравця:
//дослідження нового простору, повернення у старі місця
//повторення маршрутів і backtracking
public sealed class SpatialBehaviorTracker
{
	private const float SampleDistance = 0.35f;
	private const float CellSize = 1.25f;

	//мінімальна кількість просторових семплів,
	//після якої повернення в ту саму клітинку вважається повторним
	private const int MinSamplesBetweenVisits = 8;

	//скільки нових клітинок повинно бути відкрито
	//після попереднього відвідування, щоб повернення
	//вважалося справжнім backtracking
	private const int MinNewCellsForBacktrack = 3;

	private readonly HashSet<Vector2I> _visitedCells = new();

	//коли і скільки нових областей було відкрито
	//на момент останнього відвідування клітинки.
	private readonly Dictionary<Vector2I, VisitData> _cellVisits = new();

	private Vector2 _lastSamplePosition;
	private bool _hasSample;

	private int _sampleIndex;
	private int _newCellCount;

	public float TotalDistance { get; private set; }

	public int Samples { get; private set; }

	public int UniqueCells =>
		_visitedCells.Count;

	public int RevisitedCells { get; private set; }

	public int BacktrackEvents { get; private set; }

	public float BacktrackDistance { get; private set; }

	public int NewAreaEntries { get; private set; }

	//частка семплів, які потрапили в уже відвіданий простір
	public float RevisitRate =>
		Samples > 0
			? (float)RevisitedCells / Samples
			: 0.0f;

	//частка семплів, які відкрили новий простір
	public float NoveltyRate
	{
		get
		{
			if (Samples <= 1)
			{
				return 0.0f;
			}

			int meaningfulSamples =
				Samples - 1;

			int meaningfulNewAreas =
				Mathf.Max(
					NewAreaEntries - 1,
					0
				);

			return Mathf.Clamp(
				(float)meaningfulNewAreas /
				meaningfulSamples,
				0.0f,
				1.0f
			);
		}
	}

	public float BacktrackRate =>
		Samples > 0
			? (float)BacktrackEvents / Samples
			: 0.0f;

	//чим більша цифра, тим більше гравець схильний
	//пвертатися вже знайомим маршрутом
	public float BacktrackingTendency
	{
		get
		{
			if (Samples < 10)
			{
				return 0.0f;
			}

			float revisit =
				Mathf.Clamp(
					RevisitRate,
					0.0f,
					1.0f
				);

			float backtrack =
				Mathf.Clamp(
					BacktrackRate * 5.0f,
					0.0f,
					1.0f
				);

			return Mathf.Lerp(
				revisit,
				backtrack,
				0.65f
			);
		}
	}

	//умовний показник дослідження.
	public float ExplorationTendency =>
		Mathf.Clamp(
			NoveltyRate * 2.5f,
			0.0f,
			1.0f
		);

	public void Update(
		Vector3 worldPosition)
	{
		Vector2 currentPosition =
			new Vector2(
				worldPosition.X,
				worldPosition.Z
			);

		if (!_hasSample)
		{
			_hasSample = true;
			_lastSamplePosition = currentPosition;

			RegisterSample(currentPosition);

			return;
		}

		float distance =
			currentPosition.DistanceTo(
				_lastSamplePosition
			);

		if (distance < SampleDistance)
		{
			return;
		}

		TotalDistance += distance;
		_lastSamplePosition = currentPosition;

		RegisterSample(currentPosition);
	}

	public void Reset()
	{
		_visitedCells.Clear();
		_cellVisits.Clear();

		_lastSamplePosition = Vector2.Zero;

		_hasSample = false;

		_sampleIndex = 0;
		_newCellCount = 0;

		TotalDistance = 0.0f;

		Samples = 0;

		RevisitedCells = 0;
		BacktrackEvents = 0;
		BacktrackDistance = 0.0f;
		NewAreaEntries = 0;
	}

	private void RegisterSample(
		Vector2 position)
	{
		Samples++;
		_sampleIndex++;

		Vector2I cell =
			ToCell(position);

		bool isNewCell =
			_visitedCells.Add(cell);

		if (isNewCell)
		{
			_newCellCount++;
			NewAreaEntries++;
		}
		else if (_cellVisits.TryGetValue(
			cell,
			out VisitData previousVisit))
		{
			int sampleGap =
				_sampleIndex -
				previousVisit.LastSample;

			int newCellsSinceVisit =
				_newCellCount -
				previousVisit.NewCellCount;

			if (sampleGap >=
				MinSamplesBetweenVisits)
			{
				RevisitedCells++;

				if (newCellsSinceVisit >=
					MinNewCellsForBacktrack)
				{
					BacktrackEvents++;

					BacktrackDistance +=
						CalculateBacktrackDistance(
							cell
						);
				}
			}
		}

		_cellVisits[cell] =
			new VisitData(
				_sampleIndex,
				_newCellCount,
				position
			);
	}

	private Vector2I ToCell(
		Vector2 position)
	{
		return new Vector2I(
			Mathf.FloorToInt(
				position.X / CellSize
			),
			Mathf.FloorToInt(
				position.Y / CellSize
			)
		);
	}

	private float CalculateBacktrackDistance(
		Vector2I cell)
	{
		if (!_cellVisits.TryGetValue(
			cell,
			out VisitData visit))
		{
			return 0.0f;
		}

		return visit.Position.DistanceTo(
			_lastSamplePosition
		);
	}

	private readonly record struct VisitData(
		int LastSample,
		int NewCellCount,
		Vector2 Position
	);
}
