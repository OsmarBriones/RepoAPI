#nullable enable
using System;
using RepoAPI.Game;

namespace RepoAPI.Items
{
	/// <summary>
	/// Represents a weighted entry for item selection and drops.
	/// </summary>
	public sealed class WeightedKey
	{
		public string Key { get; }
		public float Weight { get; }

		public WeightedKey(string key, float weight)
		{
			Key = key ?? throw new ArgumentNullException(nameof(key));
			Weight = weight;
		}

		public WeightedKey(ItemName itemName, float weight)
			: this(itemName.GetGameKey(), weight)
		{
		}

		public void Deconstruct(out string key, out float weight)
		{
			key = Key;
			weight = Weight;
		}
	}
}
