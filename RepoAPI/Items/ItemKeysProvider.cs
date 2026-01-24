using System;
using System.Collections.Generic;
using System.Linq;

namespace RepoAPI.Items
{
	/// <summary>
	/// Provides random selection of <see cref="ItemName"/> keys.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This type supports two selection modes:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// <description>
	/// <b>Uniform</b>: use <see cref="GetRandomKey"/> to pick a random key from <see cref="AllKeys"/>
	/// where all keys have equal probability.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <b>Weighted</b>: create an instance of <see cref="ItemKeysProvider"/> with a weights dictionary and call
	/// <see cref="GetWeightedRandomKey"/>. Higher weights increase the chance of being selected.
	/// </description>
	/// </item>
	/// </list>
	/// <para>
	/// The instance maintains a cached internal selector for performance. When weights are changed through
	/// <see cref="SetWeights"/>, <see cref="SetWeight"/>, or <see cref="RemoveWeight"/>, the selector is rebuilt once.
	/// </para>
	/// <para>
	/// Threading: selection uses a shared <see cref="Random"/> guarded by a lock. This makes calls safe across threads,
	/// but also serializes random number generation.
	/// </para>
	/// <example>
	/// <code>
	/// // Uniform selection (string game key):
	/// string key = ItemKeysProvider.GetRandomKey();
	///
	/// // Weighted selection (ItemName):
	/// var provider = new ItemKeysProvider(new Dictionary&lt;ItemName, float&gt;
	/// {
	///     [ItemName.CartCannon] = 5f,
	///     [ItemName.CartLaser] = 1f,
	/// });
	///
	/// ItemName weighted = provider.GetWeightedRandomKey();
	///
	/// // Update weights at runtime:
	/// provider.SetWeight(ItemName.CartLaser, 10f);
	/// provider.RemoveWeight(ItemName.CartCannon);
	/// </code>
	/// </example>
	/// </remarks>
	/// <param name="weights">
	/// Initial weights used for weighted selection. Keys with weight &lt;= 0 are ignored.
	/// If <see langword="null"/> is provided, weighted selection will throw until weights are set.
	/// </param>
	public class ItemKeysProvider(Dictionary<ItemName, float> weights)
	{
		//------------ General
		private static readonly Random s_random = new();
		public static string[] AllKeys = GameKeyEnumExtensions.GetAllGameKeys<ItemName>();

		//------------ Weighted
		private WeightedKeySelector _selector = new(weights);

		/// <summary>
		/// Returns a copy of the current weights.
		/// </summary>
		/// <remarks>
		/// Modifying the returned dictionary does not affect selection until you apply it via <see cref="SetWeights"/>.
		/// Use <see cref="SetWeight"/> / <see cref="RemoveWeight"/> for single-item edits.
		/// </remarks>
		public Dictionary<ItemName, float> GetWeights() => new(_selector.Weights);

		/// <summary>
		/// Replaces the entire weights dictionary and rebuilds the internal selector.
		/// </summary>
		/// <param name="weights">New weights. Keys with weight &lt;= 0 are ignored during selection.</param>
		public void SetWeights(Dictionary<ItemName, float> weights) => _selector = new WeightedKeySelector(weights);

		/// <summary>
		/// Sets (adds or replaces) the weight for a single item and rebuilds the internal selector.
		/// </summary>
		/// <param name="itemName">The item whose weight will be set.</param>
		/// <param name="weight">
		/// The new weight. Values &lt;= 0 will effectively remove the item from selection (it will be ignored).
		/// </param>
		public void SetWeight(ItemName itemName, float weight)
		{
			var newWeights = new Dictionary<ItemName, float>(_selector.Weights);
			newWeights[itemName] = weight;
			_selector = new WeightedKeySelector(newWeights);
		}

		/// <summary>
		/// Removes an item from the weights dictionary and rebuilds the internal selector.
		/// </summary>
		/// <param name="itemName">The item to remove.</param>
		/// <returns><see langword="true"/> if the item existed and was removed; otherwise <see langword="false"/>.</returns>
		public bool RemoveWeight(ItemName itemName)
		{
			var newWeights = new Dictionary<ItemName, float>(_selector.Weights);
			if (!newWeights.Remove(itemName))
			{
				return false;
			}

			_selector = new WeightedKeySelector(newWeights);
			return true;
		}

		/// <summary>
		/// Gets a random key with uniform distribution (all keys have equal chance).
		/// </summary>
		/// <returns>
		/// A random string key from <see cref="AllKeys"/>, or <see langword="null"/> if no keys exist.
		/// </returns>
		public static string GetRandomKey()
		{
			if (AllKeys.Length == 0)
			{
				return null;
			}

			int randomIndex;
			lock (s_random)
			{
				randomIndex = s_random.Next(AllKeys.Length);
			}

			return AllKeys[randomIndex];
		}

		/// <summary>
		/// Gets a random <see cref="ItemName"/> using the current weight configuration.
		/// </summary>
		/// <remarks>
		/// Higher weight means higher probability. Items with weight &lt;= 0 are ignored.
		/// </remarks>
		/// <returns>A randomly selected <see cref="ItemName"/>.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when there are no selectable items (e.g., weights are null/empty or all weights are &lt;= 0).
		/// </exception>
		public ItemName GetWeightedRandomKey()
		{
			lock (s_random)
			{
				return _selector.GetRandomKey(s_random);
			}
		}

		private class WeightedKeySelector
		{
			public Dictionary<ItemName, float> Weights { get; }
			private readonly List<(ItemName key, float threshold)> _cumulativeWeights;
			private readonly float _totalWeight;

			public WeightedKeySelector(Dictionary<ItemName, float> weights)
			{
				Weights = weights ?? new Dictionary<ItemName, float>();

				_cumulativeWeights = new List<(ItemName, float)>(Weights.Count);
				float cumulative = 0f;

				foreach (var kvp in Weights.OrderBy(x => x.Key)) // consistent ordering
				{
					if (kvp.Value <= 0)
					{
						continue; // skip invalid weights
					}

					cumulative += kvp.Value;
					_cumulativeWeights.Add((kvp.Key, cumulative));
				}

				_totalWeight = cumulative;
			}

			public ItemName GetRandomKey(Random random)
			{
				if (_cumulativeWeights.Count == 0)
				{
					throw new InvalidOperationException("ItemKeysProvider weights have not been set.");
				}

				if (_cumulativeWeights.Count == 1)
				{
					return _cumulativeWeights[0].key;
				}

				float value = (float)(random.NextDouble() * _totalWeight);

				int index = BinarySearchThreshold(value);
				return _cumulativeWeights[index].key;
			}

			private int BinarySearchThreshold(float value)
			{
				int left = 0;
				int right = _cumulativeWeights.Count - 1;

				while (left < right)
				{
					int mid = left + ((right - left) / 2);
					if (value < _cumulativeWeights[mid].threshold)
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