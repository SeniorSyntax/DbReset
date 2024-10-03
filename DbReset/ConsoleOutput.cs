using System;

namespace DbReset;

public class ConsoleOutput : IOutput
{
	public void Info(string message)
	{
		Console.WriteLine(message);
	}

	public void Error(string message)
	{
		Console.WriteLine(message);
	}
}