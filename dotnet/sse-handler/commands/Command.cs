namespace SseHandler.Commands;

public enum Command
{
    Noop
}

public static class CommandExtensions
{
    public static string AsString(this Command command) => ((int)command).ToString();
}
