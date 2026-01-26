using BepInEx.Configuration;
using RepoAPI.Items;
using System;
using System.Collections.Generic;

namespace RepoAPI.ModConfig
{
	public static class ModConfig
	{
		public const string DefaultItemWeightsSection = "Item Weights";

		/// <summary>
		/// Creates a <see cref="ConfigEntry{T}"/> per <see cref="ItemName"/> so players can customize weights.
		/// </summary>
		/// <param name="config">BepInEx config file.</param>
		/// <param name="section">Config section name to place the entries under.</param>
		/// <param name="defaultWeight">Default weight for every item.</param>
		/// <returns>Map of item -> config entry (weight).</returns>
		public static Dictionary<ItemName, ConfigEntry<int>> BindItemWeights(ConfigFile config,
			string section = DefaultItemWeightsSection, int defaultWeight = 1)
		{
			if (config == null) throw new ArgumentNullException(nameof(config));

			var entries = new Dictionary<ItemName, ConfigEntry<int>>();

			foreach (ItemName item in ItemNames.All)
			{
				string key = item.ToString();
				entries[item] = config.Bind(
					section,
					key,
					defaultWeight,
					$"Weight for {item}. Higher = more likely. Use 0 to disable.");
			}

			return entries;
		}

		/// <summary>
		/// Creates a <see cref="ConfigEntry{T}"/> per <see cref="ItemName"/> using provided default weights.
		/// Items not present in <paramref name="defaultWeights"/> use <paramref name="fallbackDefaultWeight"/>.
		/// </summary>
		/// <param name="config">BepInEx config file.</param>
		/// <param name="defaultWeights">Per-item default weights.</param>
		/// <param name="section">Config section name to place the entries under.</param>
		/// <param name="fallbackDefaultWeight">Default weight used when an item is not present in <paramref name="defaultWeights"/>.</param>
		/// <returns>Map of item -> config entry (weight).</returns>
		public static Dictionary<ItemName, ConfigEntry<int>> BindItemWeights(ConfigFile config,
			Dictionary<ItemName, int> defaultWeights,
			string section = DefaultItemWeightsSection,
			int fallbackDefaultWeight = 1)
		{
			if (config == null) throw new ArgumentNullException(nameof(config));
			if (defaultWeights == null) throw new ArgumentNullException(nameof(defaultWeights));

			var entries = new Dictionary<ItemName, ConfigEntry<int>>();

			foreach (ItemName item in ItemNames.All)
			{
				string key = item.ToString();

				int defaultWeight = defaultWeights.TryGetValue(item, out int weight)
					? weight
					: fallbackDefaultWeight;

				entries[item] = config.Bind(
					section,
					key,
					defaultWeight,
					$"Weight for {item}. Higher = more likely. Use 0 to disable.");
			}

			return entries;
		}
	}
}
