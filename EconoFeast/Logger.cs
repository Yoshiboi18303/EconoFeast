namespace ThingBot;

internal static class Logger
{
    private static void LogWithColor(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        LogWithColor($"[INFO] {message}", color);
    }

    public static void Warning(string message, ConsoleColor color = ConsoleColor.Yellow)
    {
        LogWithColor($"[WARNING] {message}", color);
    }

    public static void Error(string message, ConsoleColor color = ConsoleColor.Red)
    {
        LogWithColor($"[ERROR] {message}", color);
    }

    public static void Success(string message, ConsoleColor color = ConsoleColor.Green)
    {
        LogWithColor($"[SUCCESS] {message}", color);
    }
}
