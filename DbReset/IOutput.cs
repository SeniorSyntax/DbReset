namespace DbReset;

public interface IOutput
{
	void Info(string message);
	void Error(string message);
}