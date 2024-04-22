namespace SseHandler.Commands;

public enum Command
{
    Noop,
    CallForSchedule
}

public static class CommandExtensions
{
    public static string AsString(this Command command) => ((int)command).ToString();
}
