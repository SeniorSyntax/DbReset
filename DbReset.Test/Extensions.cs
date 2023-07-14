using System;
using System.Collections.Generic;

namespace DbReset.Test
{
	internal static class Extensions
	{
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (T item in source)
				action(item);
		}
	}
}
