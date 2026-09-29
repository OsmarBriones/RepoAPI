#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepoAPI.Items;
using System;
using System.Collections.Generic;

namespace RepoAPI.Test
{
	[TestClass]
	public sealed class ItemKeysProviderTests
	{
		[TestMethod]
		public void GetWeightedRandomKey_WhenWeightsIsNull_Throws()
		{
			var provider = new ItemKeysProvider((Dictionary<ItemName, float>?)null);

			Assert.ThrowsException<InvalidOperationException>(() => provider.GetWeightedRandomKey());
		}

		[TestMethod]
		public void GetWeightedRandomKey_WhenWeightsIsEmpty_Throws()
		{
			var provider = new ItemKeysProvider(new Dictionary<ItemName, float>());

			Assert.ThrowsException<InvalidOperationException>(() => provider.GetWeightedRandomKey());
		}

		[TestMethod]
		public void GetWeightedRandomKey_WhenAllWeightsAreNonPositive_Throws()
		{
			var provider = new ItemKeysProvider(new Dictionary<ItemName, float>
			{
				[ItemName.CartCannon] = 0f,
				[ItemName.CartLaser] = -5f,
			});

			Assert.ThrowsException<InvalidOperationException>(() => provider.GetWeightedRandomKey());
		}

		[TestMethod]
		public void GetWeightedRandomKey_WhenOnlyOnePositiveWeight_AlwaysReturnsThatItem()
		{
			var provider = new ItemKeysProvider(new Dictionary<ItemName, float>
			{
				[ItemName.CartCannon] = 10f,
				[ItemName.CartLaser] = 0f,
				[ItemName.CartSmall] = -1f,
			});

			for (int i = 0; i < 200; i++)
			{
				Assert.AreEqual(ItemName.CartCannon, provider.GetWeightedRandomKey());
			}
		}

		[TestMethod]
		public void GetWeights_ReturnsCopy_ModifyingCopyDoesNotAffectSelectionUntilApplied()
		{
			var provider = new ItemKeysProvider(new Dictionary<ItemName, float>
			{
				[ItemName.CartCannon] = 1f,
			});

			var copy = provider.GetWeights();
			copy[ItemName.CartLaser] = 1f; // make both selectable, ~50/50

			// Still deterministic: only CartCannon is selectable in the provider.
			for (int i = 0; i < 50; i++)
			{
				Assert.AreEqual(ItemName.CartCannon, provider.GetWeightedRandomKey());
			}

			// Apply changes, now both should appear with overwhelming probability.
			provider.SetWeights(copy);

			bool sawCannon = false;
			bool sawLaser = false;

			for (int i = 0; i < 200; i++)
			{
				var item = provider.GetWeightedRandomKey();
				if (item == ItemName.CartCannon) sawCannon = true;
				else if (item == ItemName.CartLaser) sawLaser = true;

				if (sawCannon && sawLaser)
				{
					break;
				}
			}

			Assert.IsTrue(sawCannon, "Expected to observe CartCannon after applying weights.");
			Assert.IsTrue(sawLaser, "Expected to observe CartLaser after applying weights.");
		}

		[TestMethod]
		public void SetWeight_RebuildsSelector_AndCanMakeResultDeterministic()
		{
			var provider = new ItemKeysProvider(new Dictionary<ItemName, float>
			{
				[ItemName.CartCannon] = 1f,
				[ItemName.CartLaser] = 1f,
			});

			// Make only CartLaser selectable
			provider.SetWeight(ItemName.CartCannon, 0f);
			provider.SetWeight(ItemName.CartLaser, 10f);

			for (int i = 0; i < 200; i++)
			{
				Assert.AreEqual(ItemName.CartLaser, provider.GetWeightedRandomKey());
			}
		}

		[TestMethod]
		public void RemoveWeight_WhenMissing_ReturnsFalse()
		{
			var provider = new ItemKeysProvider(new Dictionary<ItemName, float>
			{
				[ItemName.CartCannon] = 1f,
			});

			Assert.IsFalse(provider.RemoveWeight(ItemName.CartLaser));
		}

		[TestMethod]
		public void RemoveWeight_WhenPresent_RemovesAndCanMakeResultDeterministic()
		{
			var provider = new ItemKeysProvider(new Dictionary<ItemName, float>
			{
				[ItemName.CartCannon] = 1f,
				[ItemName.CartLaser] = 1f,
			});

			Assert.IsTrue(provider.RemoveWeight(ItemName.CartLaser));

			for (int i = 0; i < 200; i++)
			{
				Assert.AreEqual(ItemName.CartCannon, provider.GetWeightedRandomKey());
			}
		}

		[TestMethod]
		public void GetRandomKey_WhenAllKeysEmpty_ReturnsNull()
		{
			// Avoid relying on GameKeyEnumExtensions in tests: make this deterministic.
			var original = ItemKeysProvider.AllKeys;
			try
			{
				ItemKeysProvider.AllKeys = Array.Empty<string>();
				Assert.IsNull(ItemKeysProvider.GetRandomKey());
			}
			finally
			{
				ItemKeysProvider.AllKeys = original;
			}
		}

		[TestMethod]
		public void GetRandomKey_WhenAllKeysHasOne_ReturnsThatKey()
		{
			var original = ItemKeysProvider.AllKeys;
			try
			{
				ItemKeysProvider.AllKeys = new[] { "only-key" };

				for (int i = 0; i < 50; i++)
				{
					Assert.AreEqual("only-key", ItemKeysProvider.GetRandomKey());
				}
			}
			finally
			{
				ItemKeysProvider.AllKeys = original;
			}
		}

		[TestMethod]
		public void WeightedKey_Constructors_SetPropertiesCorrectly()
		{
			var fromString = new WeightedKey("Item Gun Shotgun", 5f);
			Assert.AreEqual("Item Gun Shotgun", fromString.Key);
			Assert.AreEqual(5f, fromString.Weight);

			var fromEnum = new WeightedKey(ItemName.GunShotgun, 3.5f);
			Assert.AreEqual("Item Gun Shotgun", fromEnum.Key);
			Assert.AreEqual(3.5f, fromEnum.Weight);

			var (k, w) = fromString;
			Assert.AreEqual("Item Gun Shotgun", k);
			Assert.AreEqual(5f, w);
		}

		[TestMethod]
		public void WeightedKey_Constructor_ThrowsOnNullKey()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new WeightedKey((string)null!, 1f));
		}

		[TestMethod]
		public void PickWeightedKey_WhenListIsNull_ReturnsNull()
		{
			Assert.IsNull(ItemKeysProvider.PickWeightedKey((IReadOnlyList<WeightedKey>?)null));
		}

		[TestMethod]
		public void PickWeightedKey_WhenListIsEmpty_ReturnsNull()
		{
			Assert.IsNull(ItemKeysProvider.PickWeightedKey(Array.Empty<WeightedKey>()));
		}

		[TestMethod]
		public void PickWeightedKey_WhenAllWeightsZeroOrNegative_ReturnsNull()
		{
			var list = new[]
			{
				new WeightedKey("Item A", 0f),
				new WeightedKey("Item B", -2f),
			};
			Assert.IsNull(ItemKeysProvider.PickWeightedKey(list));
		}

		[TestMethod]
		public void PickWeightedKey_WhenSinglePositiveWeight_AlwaysReturnsThatKey()
		{
			var list = new[]
			{
				new WeightedKey("Item Zero", 0f),
				new WeightedKey("Item Winner", 5f),
				new WeightedKey("Item Negative", -1f),
			};

			for (int i = 0; i < 100; i++)
			{
				Assert.AreEqual("Item Winner", ItemKeysProvider.PickWeightedKey(list));
			}
		}

		[TestMethod]
		public void PickWeightedKey_WhenMultiplePositiveWeights_SelectsAllOverIterations()
		{
			var list = new[]
			{
				new WeightedKey("Item Alpha", 1f),
				new WeightedKey("Item Beta", 1f),
			};

			bool sawAlpha = false;
			bool sawBeta = false;

			for (int i = 0; i < 200; i++)
			{
				var key = ItemKeysProvider.PickWeightedKey(list);
				if (key == "Item Alpha") sawAlpha = true;
				else if (key == "Item Beta") sawBeta = true;

				if (sawAlpha && sawBeta) break;
			}

			Assert.IsTrue(sawAlpha, "Expected to pick Item Alpha");
			Assert.IsTrue(sawBeta, "Expected to pick Item Beta");
		}

		[TestMethod]
		public void PickWeightedKey_FromDictionary_PicksCorrectly()
		{
			var dict = new Dictionary<string, float>
			{
				["Key1"] = 10f,
				["Key2"] = 0f,
			};

			for (int i = 0; i < 50; i++)
			{
				Assert.AreEqual("Key1", ItemKeysProvider.PickWeightedKey(dict));
			}
		}

		[TestMethod]
		public void ItemKeysProvider_FromWeightedKeys_InitializesCorrectly()
		{
			var weightedKeys = new[]
			{
				new WeightedKey(ItemName.CartCannon, 10f),
				new WeightedKey("Item Cart Laser", 0f),
			};

			var provider = new ItemKeysProvider(weightedKeys);
			Assert.AreEqual(ItemName.CartCannon, provider.GetWeightedRandomKey());
			Assert.AreEqual("Item Cart Cannon", provider.GetWeightedRandomGameKey());
		}
	}
}