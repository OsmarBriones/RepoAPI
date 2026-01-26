using RepoAPI.Utils;

namespace RepoAPI.Items
{
	public static class ItemNames
	{
		public static ItemName[] All => EnumUtils.GetValues<ItemName>();
	}
}