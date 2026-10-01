#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepoAPI.Items
{
	/// <summary>
	/// Provides reusable logic to determine if items are secured, preserve them into StatsManager across levels,
	/// and clean up unpreserved instances.
	/// </summary>
	public static class ItemPreservation
	{
		/// <summary>
		/// Determines whether an item is secured inside the truck, in a player's inventory, or held by a player.
		/// </summary>
		public static bool IsItemSecured(ItemAttributes? itemAttr)
		{
			if (itemAttr == null) return false;

			// 1. In truck room volume
			var roomCheck = itemAttr.GetComponent<RoomVolumeCheck>();
			if (roomCheck != null && roomCheck.inTruck) return true;

			// 2. Equipped or held by a player
			var equippable = itemAttr.GetComponent<ItemEquippable>();
			if (equippable != null && (equippable.IsEquipped() || equippable.isEquipped)) return true;

			var grab = itemAttr.GetComponent<PhysGrabObject>();
			if (grab != null && (grab.grabbed || grab.grabbedLocal || (grab.playerGrabbing != null && grab.playerGrabbing.Count > 0)))
			{
				return true;
			}

			// 3. Registered in player inventory spots (StatsManager)
			if (!string.IsNullOrEmpty(itemAttr.instanceName))
			{
				int hash = itemAttr.instanceName.GetHashCode();
				var stats = StatsManager.instance;
				if (stats != null)
				{
					if (stats.playerInventorySpot1.ContainsValue(hash) ||
						stats.playerInventorySpot2.ContainsValue(hash) ||
						stats.playerInventorySpot3.ContainsValue(hash))
					{
						return true;
					}
				}
			}

			// 4. Proximity to truck spawn point (safety fallback if colliders slightly miss boundary)
			if (TruckSafetySpawnPoint.instance != null)
			{
				float dist = Vector3.Distance(itemAttr.transform.position, TruckSafetySpawnPoint.instance.transform.position);
				if (dist <= 8f) return true;
			}

			return false;
		}

		/// <summary>
		/// Registers a secured item in StatsManager (itemsPurchased and itemsPurchasedTotal) so the game's
		/// native truck loading logic preserves it into subsequent levels.
		/// </summary>
		public static bool TryPreserveSecuredItem(ItemAttributes? itemAttr, out string baseItemName)
		{
			baseItemName = string.Empty;
			if (itemAttr == null) return false;

			var stats = StatsManager.instance;
			if (stats == null || stats.itemDictionary == null || stats.itemsPurchased == null || stats.itemsPurchasedTotal == null)
			{
				return false;
			}

			string instanceName = itemAttr.instanceName;
			baseItemName = itemAttr.item != null ? itemAttr.item.name : (instanceName.Contains("/") ? instanceName.Split('/')[0] : instanceName);

			if (string.IsNullOrEmpty(baseItemName) || !stats.itemDictionary.ContainsKey(baseItemName))
			{
				return false;
			}

			int currentPurchased = stats.itemsPurchased.TryGetValue(baseItemName, out int cp) ? cp : 0;
			stats.itemsPurchased[baseItemName] = currentPurchased + 1;

			int currentTotal = stats.itemsPurchasedTotal.TryGetValue(baseItemName, out int ct) ? ct : 0;
			stats.itemsPurchasedTotal[baseItemName] = currentTotal + 1;

			return true;
		}

		/// <summary>
		/// Cleans up an unpreserved item instance from StatsManager (item and itemStatBattery)
		/// so uncollected items do not linger in save data.
		/// </summary>
		public static void CleanupUnpreservedInstance(string instanceName)
		{
			if (string.IsNullOrEmpty(instanceName)) return;

			var stats = StatsManager.instance;
			if (stats == null) return;

			if (stats.item != null && stats.item.ContainsKey(instanceName))
			{
				stats.item.Remove(instanceName);
			}

			if (stats.itemStatBattery != null && stats.itemStatBattery.ContainsKey(instanceName))
			{
				stats.itemStatBattery.Remove(instanceName);
			}
		}

		/// <summary>
		/// Gets or creates a named dictionary inside StatsManager.dictionaryOfDictionaries to persist
		/// mod-specific item counts across save files and game sessions.
		/// </summary>
		public static Dictionary<string, int>? GetOrCreateStatsDictionary(string dictionaryKey)
		{
			if (string.IsNullOrEmpty(dictionaryKey)) throw new ArgumentNullException(nameof(dictionaryKey));

			var stats = StatsManager.instance;
			if (stats == null) return null;

			if (!stats.dictionaryOfDictionaries.TryGetValue(dictionaryKey, out var dict))
			{
				dict = new Dictionary<string, int>();
				stats.dictionaryOfDictionaries[dictionaryKey] = dict;
			}

			return dict;
		}

		/// <summary>
		/// Calculates the total count of preserved items recorded in a named StatsManager dictionary.
		/// </summary>
		public static int GetTotalCount(string dictionaryKey)
		{
			var dict = GetOrCreateStatsDictionary(dictionaryKey);
			if (dict == null) return 0;

			int total = 0;
			foreach (var kvp in dict)
			{
				if (kvp.Value > 0)
				{
					total += kvp.Value;
				}
			}
			return total;
		}

		/// <summary>
		/// Increments the preserved count for a specific base item name in a named StatsManager dictionary.
		/// </summary>
		public static void RecordCount(string dictionaryKey, string baseItemName)
		{
			if (string.IsNullOrEmpty(baseItemName)) return;

			var dict = GetOrCreateStatsDictionary(dictionaryKey);
			if (dict == null) return;

			dict[baseItemName] = dict.TryGetValue(baseItemName, out int current) ? current + 1 : 1;
		}

		/// <summary>
		/// Decrements the preserved count when an item instance is destroyed, consumed, or removed.
		/// </summary>
		public static bool DecrementCount(string dictionaryKey, string instanceName, out string baseItemName, out int remaining)
		{
			baseItemName = string.Empty;
			remaining = 0;
			if (string.IsNullOrEmpty(instanceName)) return false;

			baseItemName = instanceName.Contains("/") ? instanceName.Split('/')[0] : instanceName;
			var dict = GetOrCreateStatsDictionary(dictionaryKey);
			if (dict == null) return false;

			if (dict.TryGetValue(baseItemName, out int current) && current > 0)
			{
				remaining = current - 1;
				dict[baseItemName] = remaining;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Resets all counts in a named StatsManager dictionary.
		/// </summary>
		public static void ResetCounts(string dictionaryKey)
		{
			var dict = GetOrCreateStatsDictionary(dictionaryKey);
			if (dict != null && dict.Count > 0)
			{
				dict.Clear();
			}
		}
	}
}
