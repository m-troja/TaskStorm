using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace TaskStorm.Log;

public static class LoggerExtensions
{
    public static void log<T>(
        this ILogger<T> logger,
        string message,
        [CallerMemberName] string memberName = "")
    {
        var className = typeof(T).Name;
     //   logger.Log(level, "[{Class}.{Method}] {Message}", className, memberName, message);
        logger.LogDebug("[{Class}.{Method}] {Message}", className, memberName, message);
    }
}
