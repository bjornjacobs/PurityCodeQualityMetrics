using Microsoft.Extensions.Logging;

namespace PurityCodeQualityMetrics;

public class OwnLogger : ILogger
{
    public LogLevel Level { get; set; }

    private string Time => $"[{DateTime.Now.ToString("hh:mm:ss")}]";

    private static readonly IDictionary<LogLevel, ConsoleColor> Colors = new Dictionary<LogLevel, ConsoleColor>
    {
        {LogLevel.Trace, ConsoleColor.Gray},
        {LogLevel.Debug, ConsoleColor.Gray},
        {LogLevel.Information, ConsoleColor.White},
        {LogLevel.Warning, ConsoleColor.Yellow},
        {LogLevel.Error, ConsoleColor.Red},
        {LogLevel.Critical, ConsoleColor.DarkRed},
    };

    public OwnLogger()
    {
        Level = LogLevel.Warning;
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var before = Console.ForegroundColor;
        if (logLevel >= Level)
        {
            Console.ForegroundColor = Colors[logLevel];
            System.Console.WriteLine($"{Time} {formatter(state, exception)}");
        }

        Console.ForegroundColor = before;

    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }
}