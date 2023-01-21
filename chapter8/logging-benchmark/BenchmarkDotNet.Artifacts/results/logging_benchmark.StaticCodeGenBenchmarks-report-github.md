``` ini

BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22621.1105)
12th Gen Intel Core i9-12900KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2


```
|                         Method |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|------------------------------- |----------:|----------:|----------:|-------:|----------:|
|             EnabledLogWarining | 34.136 ns | 0.5452 ns | 0.5100 ns | 0.0041 |      64 B |
|         DisabledLogInformation | 28.874 ns | 0.4351 ns | 0.3857 ns | 0.0041 |      64 B |
|  EnabledSourceGeneratedLogWarn | 26.010 ns | 0.1316 ns | 0.1231 ns |      - |         - |
| DisabledSourceGeneratedLogInfo |  3.389 ns | 0.0251 ns | 0.0235 ns |      - |         - |
