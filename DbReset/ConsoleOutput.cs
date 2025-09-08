using System;

namespace DbReset;

public class ConsoleOutput : IOutput
{
	public void Info(string message)
	{
		Console.WriteLine(message);
	}
}