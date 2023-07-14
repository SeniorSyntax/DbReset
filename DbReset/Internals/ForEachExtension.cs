using System;
using System.Collections.Generic;

namespace DbReset.Internals;

internal static class ForEachExtension
{
	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (T item in source)
			action(item);
	}
}
