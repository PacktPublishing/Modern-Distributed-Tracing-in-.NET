using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace logging_benchmark;

[MemoryDiagnoser]
public class StaticCodeGenBenchmarks
{
    private static readonly ILogger<StaticCodeGenBenchmarks> Logger = TestProvider.CreateLogger<StaticCodeGenBenchmarks>(LogLevel.Warning);
    private static readonly MyValue SomeValue = new ("foobar");

    [Benchmark]
    public void EnabledLogWarining()
    {
        Logger.LogWarning("{foo}{bar}", 42, SomeValue);
    }

    [Benchmark]
    public void DisabledLogInformation()
    {
        Logger.LogInformation("{foo}{bar}", 42, SomeValue);
    }

    [Benchmark]
    public void EnabledSourceGeneratedLogWarn()
    {
        Logger.LogWarn(42, SomeValue);
    }

    [Benchmark]
    public void DisabledSourceGeneratedLogInfo() {
        Logger.LogInfo(42, SomeValue);
    }
}

static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{foo} {bar}")]
    public static partial void LogInfo(this ILogger<StaticCodeGenBenchmarks> logger, int foo, MyValue bar);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "{foo} {bar}")]
    public static partial void LogWarn(this ILogger<StaticCodeGenBenchmarks> logger, int foo, MyValue bar);
}