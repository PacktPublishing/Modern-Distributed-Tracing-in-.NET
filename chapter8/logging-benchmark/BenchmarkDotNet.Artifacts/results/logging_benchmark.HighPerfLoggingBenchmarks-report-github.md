``` ini

BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22621.1105)
12th Gen Intel Core i9-12900KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2


```
|                  Method |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|------------------------ |----------:|----------:|----------:|-------:|----------:|
|      EnabledLogWarining | 33.850 ns | 0.2872 ns | 0.2546 ns | 0.0041 |      64 B |
|  DisabledLogInformation | 29.045 ns | 0.4326 ns | 0.3835 ns | 0.0041 |      64 B |
|  EnabledHighPerfLogWarn | 28.134 ns | 0.1309 ns | 0.1224 ns |      - |         - |
| DisabledHighPerfLogInfo |  5.337 ns | 0.0534 ns | 0.0499 ns |      - |         - |
