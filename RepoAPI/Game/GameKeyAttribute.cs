using System;

namespace RepoAPI.Game
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class GameKeyAttribute(string key) : Attribute
	{
		public string Key { get; } = key;
	}
}
