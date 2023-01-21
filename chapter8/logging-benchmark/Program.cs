using BenchmarkDotNet.Running;
using logging_benchmark;

BenchmarkRunner.Run( new[] {
    typeof(SimpleLoggerBenchmarks),
    typeof(StaticCodeGenBenchmarks),
    typeof(HighPerfLoggingBenchmarks)});