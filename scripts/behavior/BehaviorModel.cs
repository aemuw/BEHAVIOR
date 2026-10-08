using System;
using System.Linq;
using Godot;

//контекстна статистична модель поведінки
//вона тільки вчиться і прогнозує
//вона не запускає horror-події
public sealed class BehaviorModel
{
	public const int MinSamplesForEvaluation = 3;

	//наскільки сильно глобальна поведінка допомагає конкретному контексту
	private const float PriorStrength = 1.0f;

	//швидкість росту довіри до кількості зразків
	private const float ConfidenceK = 6.0f;

	//наскільки сильно нові результати впливають на recent accuracy
	private const float RecentAccuracyAlpha = 0.18f;

	private static readonly int ContextCount =
		Enum.GetValues<BehaviorContext>().Length;

	private static readonly int ActionCount =
		Enum.GetValues<BehaviorAction>().Length;

	private readonly int[][] _counts;

	private readonly int[] _global;

	private readonly int[] _evaluationsByContext;
	private readonly int[] _hitsByContext;

	private readonly float[] _recentAccuracyByContext;
	private readonly float[] _brierSumByContext;

	private int _globalTotal;

	public int Evaluations { get; private set; }
	public int Hits { get; private set; }

	public bool LastWasHit { get; private set; }
	public bool HasLastEvaluation { get; private set; }

	public float LastBrierScore { get; private set; }

	public float Predictability =>
		Evaluations > 0
			? (float)Hits / Evaluations
			: 0.0f;

	public float PredictabilityAboveChance
	{
		get
		{
			float chance =
				1.0f / ActionCount;

			return Mathf.Clamp(
				(Predictability - chance) /
				(1.0f - chance),

				0.0f,
				1.0f
			);
		}
	}

	public float RecentPredictability
	{
		get
		{
			if (Evaluations == 0)
			{
				return 0.0f;
			}

			float sum = 0.0f;
			int used = 0;

			for (int i = 0;
				 i < ContextCount;
				 i++)
			{
				if (_evaluationsByContext[i] == 0)
				{
					continue;
				}

				sum +=
					_recentAccuracyByContext[i];

				used++;
			}

			return used > 0
				? sum / used
				: 0.0f;
		}
	}

	public BehaviorModel()
	{
		_counts =
			new int[ContextCount][];

		for (int i = 0;
			 i < ContextCount;
			 i++)
		{
			_counts[i] =
				new int[ActionCount];
		}

		_global =
			new int[ActionCount];

		_evaluationsByContext =
			new int[ContextCount];

		_hitsByContext =
			new int[ContextCount];

		_recentAccuracyByContext =
			new float[ContextCount];

		_brierSumByContext =
			new float[ContextCount];
	}

	public Prediction Predict(
		BehaviorContext context)
	{
		if (context == BehaviorContext.None)
		{
			return new Prediction(
				BehaviorAction.Idle,
				0.0f,
				0.0f,
				0
			);
		}

		int[] counts =
			_counts[(int)context];

		int samples =
			counts.Sum();

		float[] probabilities =
			Distribution(
				counts,
				samples
			);

		int bestIndex = 0;

		for (int i = 1;
			 i < probabilities.Length;
			 i++)
		{
			if (probabilities[i] >
				probabilities[bestIndex])
			{
				bestIndex = i;
			}
		}

		//Confidence відповідає кількості даних
		//Probability відповідає самому прогнозу
		float confidence =
			1.0f -
			Mathf.Exp(
				-samples / ConfidenceK
			);

		return new Prediction(
			(BehaviorAction)bestIndex,
			probabilities[bestIndex],
			confidence,
			samples
		);
	}

	public float[] GetDistribution(
		BehaviorContext context)
	{
		if (context ==
			BehaviorContext.None)
		{
			return UniformDistribution();
		}

		int[] counts =
			_counts[(int)context];

		return Distribution(
			counts,
			counts.Sum()
		);
	}

	public int GetSamples(
		BehaviorContext context)
	{
		return context == BehaviorContext.None
			? 0
			: _counts[(int)context].Sum();
	}

	public int GetEvaluations(
		BehaviorContext context)
	{
		return context == BehaviorContext.None
			? 0
			: _evaluationsByContext[(int)context];
	}

	public float GetPredictability(
		BehaviorContext context)
	{
		int evaluations =
			GetEvaluations(context);

		if (evaluations == 0)
		{
			return 0.0f;
		}

		return (float)
			_hitsByContext[(int)context]
			/ evaluations;
	}

	public float GetRecentPredictability(
		BehaviorContext context)
	{
		return context ==
			   BehaviorContext.None

			? 0.0f

			: _recentAccuracyByContext[
				(int)context
			  ];
	}

	public float GetAverageBrierScore(
		BehaviorContext context)
	{
		int evaluations =
			GetEvaluations(context);

		if (evaluations == 0)
		{
			return 1.0f;
		}

		return
			_brierSumByContext[
				(int)context
			] / evaluations;
	}

	//спочатку оцінюємо старий прогноз
	//потім додаємо нове спостереження
	public void Observe(
		BehaviorContext context,
		BehaviorAction action)
	{
		if (context ==
			BehaviorContext.None)
		{
			return;
		}

		int[] counts =
			_counts[(int)context];

		int samples =
			counts.Sum();

		HasLastEvaluation = false;

		if (samples >=
			MinSamplesForEvaluation)
		{
			Prediction prediction =
				Predict(context);

			float[] probabilities =
				GetDistribution(context);

			LastWasHit =
				prediction.Action == action;

			LastBrierScore =
				CalculateBrierScore(
					probabilities,
					action
				);

			HasLastEvaluation = true;

			Evaluations++;

			_evaluationsByContext[
				(int)context
			]++;

			if (LastWasHit)
			{
				Hits++;

				_hitsByContext[
					(int)context
				]++;
			}

			int contextIndex =
				(int)context;

			float target =
				LastWasHit
					? 1.0f
					: 0.0f;

			if (_evaluationsByContext[
					contextIndex] == 1)
			{
				_recentAccuracyByContext[
					contextIndex] =
					target;
			}
			else
			{
				_recentAccuracyByContext[
					contextIndex] =
					Mathf.Lerp(
						_recentAccuracyByContext[
							contextIndex],
						target,
						RecentAccuracyAlpha
					);
			}

			_brierSumByContext[
				contextIndex
			] += LastBrierScore;
		}

		counts[(int)action]++;

		_global[(int)action]++;

		_globalTotal++;
	}

	private float[] Distribution(
		int[] counts,
		int samples)
	{
		float[] probabilities =
			new float[ActionCount];

		for (int i = 0;
			 i < ActionCount;
			 i++)
		{
			//на старті prior майже рівномірний
			//потім трохи враховує загальний стиль гравця
			float prior =
				(_global[i] + 1.0f) /
				(_globalTotal + ActionCount);

			probabilities[i] =
				(counts[i] +
				 PriorStrength * prior)
				/
				(samples +
				 PriorStrength);
		}

		return probabilities;
	}

	private static float CalculateBrierScore(
		float[] probabilities,
		BehaviorAction actualAction)
	{
		float score = 0.0f;

		for (int i = 0;
			 i < probabilities.Length;
			 i++)
		{
			float expected =
				i == (int)actualAction
					? 1.0f
					: 0.0f;

			float error =
				probabilities[i] -
				expected;

			score +=
				error * error;
		}

		return score;
	}

	private float[] UniformDistribution()
	{
		float[] probabilities =
			new float[ActionCount];

		float probability =
			1.0f / ActionCount;

		for (int i = 0;
			 i < probabilities.Length;
			 i++)
		{
			probabilities[i] =
				probability;
		}

		return probabilities;
	}
}
