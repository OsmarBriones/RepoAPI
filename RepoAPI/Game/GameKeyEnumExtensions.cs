using RepoAPI.Game;
using System;
using System.Linq;
using System.Reflection;

public static class GameKeyEnumExtensions
{
	/// <summary>
	/// Gets the game key string from any enum decorated with [GameKey].
	/// </summary>
	public static string GetGameKey<T>(this T value) where T : Enum
	{
		var member = typeof(T).GetMember(value.ToString()).First();
		var attr = member.GetCustomAttribute<GameKeyAttribute>();
		return attr?.Key ?? value.ToString();
	}

	/// <summary>
	/// Attempts to map a game key string back into the enum.
	/// </summary>
	public static bool TryFromGameKey<T>(string key, out T result) where T : Enum
	{
		foreach (var val in Enum.GetValues(typeof(T)).Cast<T>())
		{
			if (val.GetGameKey().Equals(key, StringComparison.OrdinalIgnoreCase))
			{
				result = val;
				return true;
			}
		}

		result = default;
		return false;
	}

	public static string[] GetAllGameKeys<T>() where T : Enum
	{
		return Enum.GetValues(typeof(T))
			.Cast<T>()
			.Select(v => v.GetGameKey())
			.ToArray();
	}

	/// <summary>
	/// Converts a game key string into an enum value or throws.
	/// </summary>
	public static T FromGameKey<T>(string key) where T : Enum
	{
		if (TryFromGameKey<T>(key, out var value))
			return value;

		throw new ArgumentException($"Invalid game key '{key}' for enum {typeof(T).Name}");
	}
}
