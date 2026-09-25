using Photon.Pun;
using System;
using UnityEngine;

namespace RepoAPI.Items
{
	/// <summary>
	/// Provides spawning of upgrade items using keys from ItemKeysProvider and StatsManager.itemDictionary.
	/// </summary>
	public static class ItemProvider
	{
		/// <summary>
		/// Attempts to spawn a random item at the given position (delegates to TrySpawnByKey).
		/// </summary>
		public static bool TrySpawnRandomItem(Vector3 position, Quaternion rotation, out GameObject spawned, float upwardOffset = 0.15f)
		{
			spawned = null;

			string key = ItemKeysProvider.GetRandomKey();
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}

			return TrySpawnByKey(key, position, rotation, out spawned, upwardOffset);
		}

		/// <summary>
		/// Attempts to spawn a specific item by dictionary key.
		/// </summary>
		public static bool TrySpawnByKey(string key, Vector3 position, Quaternion rotation, out GameObject spawned, float upwardOffset = 0.15f)
		{
			spawned = null;
			if(!SemiFunc.IsMasterClientOrSingleplayer()) return false;

			var dict = StatsManager.instance.itemDictionary;

			if (!dict.TryGetValue(key, out var item) || item == null || item.prefab == null)
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

		private static void ApplySpawnImpulse(GameObject go)
		{
			if (!go) return;
			if (go.TryGetComponent<Rigidbody>(out var rb))
			{
				rb.AddForce(UnityEngine.Random.insideUnitSphere * 1.25f + Vector3.up * 2f, ForceMode.Impulse);
			}
		}
	}
}