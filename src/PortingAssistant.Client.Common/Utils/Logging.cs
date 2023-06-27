using Microsoft.Extensions.Logging;

namespace PortingAssistant.Client.Common.Utils;

public class TraceEvent
{
    private static bool _disabledMetrics = false;

    public static void Start(ILogger logger, string eventDescription)
    {
        if (_disabledMetrics) { return; }
        logger.LogInformation($"Starting: {eventDescription}");
    }

    public static void End(ILogger logger, string eventDescription)
    {
        if (_disabledMetrics) { return; }
        logger.LogInformation($"Complete: {eventDescription}");
    }

    public static void Start(Serilog.ILogger logger, string eventDescription)
    {
        if (_disabledMetrics) { return; }
        logger.Information($"Starting: {eventDescription}");
    }

    public static void End(Serilog.ILogger logger, string eventDescription)
    {
        if (_disabledMetrics) { return; }
        logger.Information($"Complete: {eventDescription}");
    }

    public static void ToggleMetrics(bool disabledMetrics)
    {
        _disabledMetrics = disabledMetrics;
    }
}