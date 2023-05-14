using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace logging_benchmark;

[MemoryDiagnoser]
public class HighPerfLoggingBenchmarks
{
    private static readonly ILogger<HighPerfLoggingBenchmarks> Logger =
        TestProvider.CreateLogger<HighPerfLoggingBenchmarks>(LogLevel.Warning);

    private static readonly Action<ILogger, int, MyValue, Exception> LogWarn = LoggerMessage.Define<int, MyValue>(
        LogLevel.Warning, new EventId(3), "{foo} {bar}");

    private static readonly Action<ILogger, int, MyValue, Exception> LogInfo = LoggerMessage.Define<int, MyValue>(
        LogLevel.Information, new EventId(4), "{foo} {bar}");

    private static readonly MyValue SomeValue = new ("foobar");

    [Benchmark]
    public void EnabledLogWarning()
    {
        Logger.LogWarning("{foo}{bar}", 42, SomeValue);
    }

    [Benchmark]
    public void DisabledLogInformation()
    {
        Logger.LogInformation("{foo}{bar}", 42, SomeValue);
    }

    [Benchmark]
    public void EnabledHighPerfLogWarn()
    {
        // assumption is that SomeValue is created regardless of logging
        // otherwise, it's creation could be guarded with IsEnabled check.
        LogWarn(Logger, 42, SomeValue, default!);
    }

    [Benchmark]
    public void DisabledHighPerfLogInfo()
    {
        LogInfo(Logger, 42, SomeValue, default!);
    }
}