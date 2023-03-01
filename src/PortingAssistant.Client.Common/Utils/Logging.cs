using Microsoft.Extensions.Logging;

namespace PortingAssistant.Client.Common.Utils;

public class TraceEvent
{
    public static void Start(ILogger logger, string eventDescription)
    {
        logger.LogInformation($"Starting: {eventDescription}");
    }

    public static void End(ILogger logger, string eventDescription)
    {
        logger.LogInformation($"Complete: {eventDescription}");
    }

    public static void Start(Serilog.ILogger logger, string eventDescription)
    {
        logger.Information($"Starting: {eventDescription}");
    }

    public static void End(Serilog.ILogger logger, string eventDescription)
    {
        logger.Information($"Complete: {eventDescription}");
    }
}