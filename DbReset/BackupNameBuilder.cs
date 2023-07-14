using System;
using System.Collections.Generic;
using System.Linq;
using DbReset.Internals;

namespace DbReset;

public static class BackupNameBuilder
{
	private const string extension = "dbcache";
	private static readonly IEnumerable<int> keyCutoffs = new[] { int.MaxValue, 32, 16, 1 };

	public static IEnumerable<string> PossibleKeysForKey(string key) =>
		keysForKey(key, keyCutoffs);

	private static IEnumerable<string> possibleNames(string key, string version)
	{
		var possibleContentVersion = keysForKey(version, new[] { int.MaxValue, 1 });
		return PossibleKeysForKey(key)
			.SelectMany(_ => possibleContentVersion, (k, v) => new { k, v })
			.OrderByDescending(x => x.k.Length)
			.ThenByDescending(x => x.v?.Length)
			.Select(x => $"{x.k}{x.v}{extension}")
			.ToArray();
	}

	internal static string GetNameWithMaxLength(string key, string version, int maxLength) =>
		possibleNames(key, version)
			.First(x => x.Length <= maxLength);

	public static string Extension() => $".{extension}";
	public static string TempFolder() => @"c:\temp\dbcache";

	private static IEnumerable<string> keysForKey(string key, IEnumerable<int> cutOffs)
	{
		if (key == null)
			return new[] { key };

		var hash = key.GetDeterministicHashCode().ToString();
		hash = $".{hash}";
		return (
				from c in cutOffs
				let cut = Math.Min(key.Length, c)
				let k = key.Substring(0, cut)
				let h = key == k ? null : hash
				select $"{k}{h}."
			)
			.Distinct()
			.ToArray();
	}
}
