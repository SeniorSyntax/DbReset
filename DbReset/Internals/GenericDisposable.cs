using System;

namespace DbReset.Internals;

internal class GenericDisposable : IDisposable
{
	private Action _disposeAction;

	public GenericDisposable(Action disposeAction)
	{
		_disposeAction = disposeAction;
	}

	public void Dispose()
	{
		_disposeAction?.Invoke();
		_disposeAction = null;
	}
}
