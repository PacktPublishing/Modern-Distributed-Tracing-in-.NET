using Microsoft.Extensions.Logging;

namespace LegacyServiceBOTel
{
    internal static class LoggingHelpers
    {
        public static ILoggerFactory CreateFactory()
        {
            return LoggerFactory.Create(b => 
                b.AddJsonConsole(o =>
                {
                    o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";
                    o.UseUtcTimestamp = true;
                    o.IncludeScopes = true;
                }));
        }
    }
}
