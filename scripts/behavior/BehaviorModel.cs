using System;
using System.Linq;

public class BehaviorModel
{
	//скільки спостережень контексту потрібно, щоб прогноз рахувався в Predictability
	private const int MinSamplesForEvaluation = 2;

	//сила початкового припущення (чим більше, тим повільніше модель вчиться)
	private const float PriorStrength = 2.0f;

	//впевненість = n / (n + ConfidenceK)
	private const float ConfidenceK = 4.0f;

	private static readonly int ContextCount = Enum.GetValues<BehaviorContext>().Length;
	private static readonly int ActionCount = Enum.GetValues<BehaviorAction>().Length;

	private readonly int[][] _counts;
	private readonly int[] _global;
	private int _globalTotal;

	public int Evaluations { get; private set; }
	public int Hits { get; private set; }
	public bool LastWasHit { get; private set; }
	public bool HasLastEvaluation { get; private set; }

	//частка прогнозів, які справдилися
	public float Predictability => Evaluations > 0 ? (float)Hits / Evaluations : 0.0f;

	//те саме, але з вирахуванням випадкового вгадування (1 / кількість дій)
	public float PredictabilityAboveChance
	{
		get
		{
			float chance = 1.0f / ActionCount;
			return Math.Max(0.0f, (Predictability - chance) / (1.0f - chance));
		}
	}

	public BehaviorModel()
	{
		_counts = new int[ContextCount][];
		for (int i = 0; i < ContextCount; i++)
		{
			_counts[i] = new int[ActionCount];
		}
		_global = new int[ActionCount];
	}

	public Prediction Predict(BehaviorContext context)
	{
		int[] counts = _counts[(int)context];
		int n = counts.Sum();
		float[] p = Distribution(counts, n);

		int best = 0;
		for (int i = 1; i < p.Length; i++)
		{
			if (p[i] > p[best])
			{
				best = i;
			}
		}

		return new Prediction((BehaviorAction)best, p[best], n / (n + ConfidenceK), n);
	}

	//повний розподіл ймовірностей для контексту 
	public float[] GetDistribution(BehaviorContext context)
	{
		int[] counts = _counts[(int)context];
		return Distribution(counts, counts.Sum());
	}

	//спочатку оцінюємо точність прогнозу, потім навчаємося на новому спостереженні
	public void Observe(BehaviorContext context, BehaviorAction action)
	{
		int[] counts = _counts[(int)context];
		int n = counts.Sum();

		HasLastEvaluation = false;
		if (n >= MinSamplesForEvaluation)
		{
			Prediction prediction = Predict(context);
			LastWasHit = prediction.Action == action;
			HasLastEvaluation = true;
			Evaluations++;
			if (LastWasHit)
			{
				Hits++;
			}
		}

		counts[(int)action]++;
		_global[(int)action]++;
		_globalTotal++;
	}

	private float[] Distribution(int[] counts, int n)
	{
		float[] p = new float[ActionCount];
		for (int i = 0; i < ActionCount; i++)
		{
			float prior = (_global[i] + 1.0f) / (_globalTotal + ActionCount);
			p[i] = (counts[i] + PriorStrength * prior) / (n + PriorStrength);
		}
		return p;
	}
}
