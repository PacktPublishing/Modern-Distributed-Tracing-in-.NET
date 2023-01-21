using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace logging_benchmark;

[MemoryDiagnoser]
public class SimpleLoggerBenchmarks
{
    private static readonly ILogger<SimpleLoggerBenchmarks> Logger = TestProvider.CreateLogger<SimpleLoggerBenchmarks>(LogLevel.Warning);

    [Benchmark]
    public void EnabledLogWarningNoParams() => Logger.LogWarning("foobar");

    [Benchmark]
    public void DisabledLogInformationNoParams() => Logger.LogInformation("foobar");


    [Benchmark]
    public void EnabledLogWarningWithParams() => Logger.LogWarning("{foo}{bar}", "42", "bar");

    [Benchmark]
    public void DisbledLogInformationWithParams() => Logger.LogInformation("{foo}{bar}", "42", "bar");

    [Benchmark]
    public void EnabledLogWarningWithParamsCalculation() => Logger.LogWarning("{foo}{bar}", 42.ToString(), "foobar"[3..]);


    [Benchmark]
    public void DisabledLogInformationWithParamsCalculation()
    {
        Logger.LogInformation("{foo}{bar}", 42.ToString(), "foobar"[3..]);
    }

    [Benchmark]
    public void DisabledLogInformationWithEnabledCheck()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("{foo}{bar}", 42.ToString(), "foobar"[3..]);
        }
    }
}