using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RepoAPI.Items
{
	public class Item
	{

		public GameObject Spawn(ItemName itemName, Vector3 position, Quaternion? rotation = null, float upwardOffset = 1)
		{
			string itemKey = itemName.GetGameKey();
			return Spawn(itemKey, position, rotation, upwardOffset);
		}

		public GameObject Spawn(String itemName, Vector3 position, Quaternion? rotation = null, float upwardOffset = 1)
		{
			// Validate that the provided string maps to a defined ItemName enum member
			bool isValidItemName = GameKeyEnumExtensions.TryFromGameKey<ItemName>(itemName, out _);
			if (!isValidItemName)
			{
				throw new ArgumentException(nameof(itemName), $"Invalid item name '{itemName}': not a member of ItemName enum.");
			}

			Quaternion cleanRotation = (rotation == null) ? Quaternion.identity : (Quaternion)rotation;
			ItemProvider.TrySpawnByKey(itemName, position, cleanRotation, out GameObject spawned, upwardOffset);
			return spawned;
		}
	}
}
