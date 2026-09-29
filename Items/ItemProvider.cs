#nullable enable
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RepoAPI.Game;

namespace RepoAPI.Items
{
	/// <summary>
	/// Provides spawning of items using keys from ItemKeysProvider and StatsManager.itemDictionary.
	/// Supports uniform random spawning, specific key/enum spawning, and weighted probability drops.
	/// </summary>
	public static class ItemProvider
	{
		private static readonly System.Random s_rng = new();

		/// <summary>
		/// Attempts to spawn a random item at the given position with uniform distribution.
		/// </summary>
		public static bool TrySpawnRandomItem(Vector3 position, Quaternion rotation, out GameObject? spawned, float upwardOffset = 0.15f)
		{
			spawned = null;

			string? key = ItemKeysProvider.GetRandomKey();
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}

			return TrySpawnByKey(key!, position, rotation, out spawned, upwardOffset);
		}

		/// <summary>
		/// Attempts to spawn a specific item by <see cref="ItemName"/> enum value.
		/// </summary>
		public static bool TrySpawn(ItemName itemName, Vector3 position, Quaternion rotation, out GameObject? spawned, float upwardOffset = 0.15f)
		{
			return TrySpawnByKey(itemName.GetGameKey(), position, rotation, out spawned, upwardOffset);
		}

		/// <summary>
		/// Attempts to spawn a specific item by dictionary key string.
		/// </summary>
		public static bool TrySpawnByKey(string key, Vector3 position, Quaternion rotation, out GameObject? spawned, float upwardOffset = 0.15f)
		{
			spawned = null;
			if (!SemiFunc.IsMasterClientOrSingleplayer()) return false;

			var stats = StatsManager.instance;
			if (stats == null) return false;

			var dict = stats.itemDictionary;
			if (dict == null || !dict.TryGetValue(key, out var item) || item == null || item.prefab == null)
			{
				return false;
			}

			Vector3 spawnPos = position + Vector3.up * upwardOffset;
			try
			{
				if (SemiFunc.IsMultiplayer())
				{
					spawned = PhotonNetwork.InstantiateRoomObject(item.prefab.ResourcePath, spawnPos, rotation, 0);
				}
				else
				{
					spawned = UnityEngine.Object.Instantiate(item.prefab.Prefab, spawnPos, rotation);
				}
			}
			catch (Exception)
			{
				return false;
			}

			ApplySpawnImpulse(spawned);
			return true;
		}

		/// <summary>
		/// Attempts to pick a key from a weighted list and spawn it.
		/// If <paramref name="dropChance"/> is less than 1.0, rolls probability before picking/spawning.
		/// </summary>
		public static bool TrySpawnWeightedItem(
			IReadOnlyList<WeightedKey> weights,
			Vector3 position,
			Quaternion rotation,
			out GameObject? spawned,
			float upwardOffset = 0.15f,
			float dropChance = 1f)
		{
			spawned = null;

			if (!EvaluateDropChance(dropChance))
			{
				return false;
			}

			string? key = ItemKeysProvider.PickWeightedKey(weights, s_rng);
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}

			return TrySpawnByKey(key!, position, rotation, out spawned, upwardOffset);
		}

		/// <summary>
		/// Attempts to pick a key from an enumerable sequence of weighted keys and spawn it.
		/// If <paramref name="dropChance"/> is less than 1.0, rolls probability before picking/spawning.
		/// </summary>
		public static bool TrySpawnWeightedItem(
			IEnumerable<WeightedKey> weights,
			Vector3 position,
			Quaternion rotation,
			out GameObject? spawned,
			float upwardOffset = 0.15f,
			float dropChance = 1f)
		{
			if (weights is IReadOnlyList<WeightedKey> list)
			{
				return TrySpawnWeightedItem(list, position, rotation, out spawned, upwardOffset, dropChance);
			}

			return TrySpawnWeightedItem(weights.ToList(), position, rotation, out spawned, upwardOffset, dropChance);
		}

		/// <summary>
		/// Attempts to pick a key using an <see cref="ItemKeysProvider"/> instance and spawn it.
		/// If <paramref name="dropChance"/> is less than 1.0, rolls probability before picking/spawning.
		/// </summary>
		public static bool TrySpawnWeightedItem(
			ItemKeysProvider provider,
			Vector3 position,
			Quaternion rotation,
			out GameObject? spawned,
			float upwardOffset = 0.15f,
			float dropChance = 1f)
		{
			spawned = null;
			if (provider == null) return false;

			if (!EvaluateDropChance(dropChance))
			{
				return false;
			}

			try
			{
				ItemName item = provider.GetWeightedRandomKey();
				return TrySpawn(item, position, rotation, out spawned, upwardOffset);
			}
			catch (InvalidOperationException)
			{
				return false;
			}
		}

		/// <summary>
		/// Attempts to pick a key from an item-weight dictionary and spawn it.
		/// If <paramref name="dropChance"/> is less than 1.0, rolls probability before picking/spawning.
		/// </summary>
		public static bool TrySpawnWeightedItem(
			Dictionary<ItemName, float> weights,
			Vector3 position,
			Quaternion rotation,
			out GameObject? spawned,
			float upwardOffset = 0.15f,
			float dropChance = 1f)
		{
			var provider = new ItemKeysProvider(weights);
			return TrySpawnWeightedItem(provider, position, rotation, out spawned, upwardOffset, dropChance);
		}

		/// <summary>
		/// Attempts to pick a key from a string-weight dictionary and spawn it.
		/// If <paramref name="dropChance"/> is less than 1.0, rolls probability before picking/spawning.
		/// </summary>
		public static bool TrySpawnWeightedItem(
			Dictionary<string, float> weights,
			Vector3 position,
			Quaternion rotation,
			out GameObject? spawned,
			float upwardOffset = 0.15f,
			float dropChance = 1f)
		{
			spawned = null;
			if (weights == null || weights.Count == 0) return false;

			if (!EvaluateDropChance(dropChance))
			{
				return false;
			}

			string? key = ItemKeysProvider.PickWeightedKey(weights, s_rng);
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}

			return TrySpawnByKey(key!, position, rotation, out spawned, upwardOffset);
		}

		private static bool EvaluateDropChance(float dropChance)
		{
			if (dropChance <= 0f) return false;
			if (dropChance >= 1f) return true;

			double roll;
			lock (s_rng)
			{
				roll = s_rng.NextDouble();
			}

			return roll <= dropChance;
		}

		private static void ApplySpawnImpulse(GameObject? go)
		{
			if (go is null || !go) return;
			if (go.TryGetComponent<Rigidbody>(out var rb))
			{
				rb.AddForce(UnityEngine.Random.insideUnitSphere * 1.25f + Vector3.up * 2f, ForceMode.Impulse);
			}
		}
	}
}