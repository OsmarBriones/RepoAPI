#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RepoAPI.Items
{
	/// <summary>
	/// Provides random selection of <see cref="ItemName"/> keys.
	/// </summary>
	public class ItemKeysProvider(Dictionary<ItemName, float>? weights)
	{
		//------------ General
		private static readonly Random random = new();
		public static string[] AllKeys = GameKeyEnumExtensions.GetAllGameKeys<ItemName>();

		//------------ Weighted
		private WeightedKeySelector selector = new(weights);

		/// <summary>
		/// Returns a copy of the current weights.
		/// </summary>
		public Dictionary<ItemName, float> GetWeights() => new(selector.Weights);

		/// <summary>
		/// Replaces the entire weights dictionary and rebuilds the internal selector.
		/// </summary>
		public void SetWeights(Dictionary<ItemName, float>? weights) => selector = new WeightedKeySelector(weights);

		/// <summary>
		/// Sets (adds or replaces) the weight for a single item and rebuilds the internal selector.
		/// </summary>
		public void SetWeight(ItemName itemName, float weight)
		{
			var newWeights = new Dictionary<ItemName, float>(selector.Weights);
			newWeights[itemName] = weight;
			selector = new WeightedKeySelector(newWeights);
		}

		/// <summary>
		/// Removes an item from the weights dictionary and rebuilds the internal selector.
		/// </summary>
		public bool RemoveWeight(ItemName itemName)
		{
			var newWeights = new Dictionary<ItemName, float>(selector.Weights);
			if (!newWeights.Remove(itemName))
			{
				return false;
			}

			selector = new WeightedKeySelector(newWeights);
			return true;
		}

		/// <summary>
		/// Gets a random key with uniform distribution (all keys have equal chance).
		/// </summary>
		public static string? GetRandomKey()
		{
			if (AllKeys.Length == 0)
			{
				return null;
			}

			int randomIndex;
			lock (random)
			{
				randomIndex = random.Next(AllKeys.Length);
			}

			return AllKeys[randomIndex];
		}

		/// <summary>
		/// Gets a random <see cref="ItemName"/> using the current weight configuration.
		/// </summary>
		public ItemName GetWeightedRandomKey()
		{
			lock (random)
			{
				return selector.GetRandomKey(random);
			}
		}

		private class WeightedKeySelector
		{
			public Dictionary<ItemName, float> Weights { get; }
			private readonly List<(ItemName key, float threshold)> cumulativeWeights;
			private readonly float totalWeight;

			public WeightedKeySelector(Dictionary<ItemName, float>? weights)
			{
				Weights = weights ?? new Dictionary<ItemName, float>();

				cumulativeWeights = new List<(ItemName, float)>(Weights.Count);
				float cumulative = 0f;

				foreach (var kvp in Weights.OrderBy(x => x.Key))
				{
					if (kvp.Value <= 0)
					{
						continue;
					}

					cumulative += kvp.Value;
					cumulativeWeights.Add((kvp.Key, cumulative));
				}

				totalWeight = cumulative;
			}

			public ItemName GetRandomKey(Random rng)
			{
				if (cumulativeWeights.Count == 0)
				{
					throw new InvalidOperationException("ItemKeysProvider weights have not been set.");
				}

				if (cumulativeWeights.Count == 1)
				{
					return cumulativeWeights[0].key;
				}

				float value = (float)(rng.NextDouble() * totalWeight);
				int index = BinarySearchThreshold(value);
				return cumulativeWeights[index].key;
			}

			private int BinarySearchThreshold(float value)
			{
				int left = 0;
				int right = cumulativeWeights.Count - 1;

				while (left < right)
				{
					int mid = left + ((right - left) / 2);
					if (value < cumulativeWeights[mid].threshold)
					{
						right = mid;
					}
					else
					{
						left = mid + 1;
					}
				}

				return left;
			}
		}
	}
}