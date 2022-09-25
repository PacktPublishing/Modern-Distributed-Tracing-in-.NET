using Microsoft.Extensions.Logging;
namespace common;

public static class DiagnosticsExtensions
{
    public static ILoggingBuilder ConfigureLogs(this ILoggingBuilder loggingBuilder)
    {
        return loggingBuilder.Configure(o => o.ActivityTrackingOptions = 
           ActivityTrackingOptions.TraceId |
           ActivityTrackingOptions.SpanId);
    }
}
