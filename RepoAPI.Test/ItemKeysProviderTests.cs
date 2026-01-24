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
			var provider = new ItemKeysProvider(null);

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
	}
}