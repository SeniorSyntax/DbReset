using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DbReset.Test
{
	public class ConcurrencyRunner
	{
		private class task
		{
			public Thread Thread;
			public Exception Exception;
		}

		private readonly List<task> _tasks = new();
		private Action _lastAsyncAction;


		public void InParallel(Action action)
		{
			_lastAsyncAction = action;
			addTask(_lastAsyncAction);
		}

		private void addTask(Action action)
		{
			var task = new task();
			task.Thread = new Thread(() =>
			{
				try
				{
					action();
				}
				catch (Exception e)
				{
					task.Exception = e;
				}
			});
			task.Thread.Start();
			_tasks.Add(task);
		}

		public void Wait()
		{
			_tasks.ForEach(t => t.Thread.Join());
			var exceptions = _tasks
				.Where(t => t.Exception != null)
				.Select(t => t.Exception)
				.ToArray();
			if (exceptions.Any())
				throw new AggregateException(exceptions);
		}
	}
}
