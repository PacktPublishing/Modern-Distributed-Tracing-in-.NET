``` ini

BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22621.1105)
12th Gen Intel Core i9-12900KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2


```
|                                      Method |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|-------------------------------------------- |----------:|----------:|----------:|-------:|----------:|
|                   EnabledLogWarningNoParams | 21.573 ns | 0.1325 ns | 0.1174 ns |      - |         - |
|              DisabledLogInformationNoParams | 14.229 ns | 0.1579 ns | 0.1477 ns |      - |         - |
|                 EnabledLogWarningWithParams | 32.444 ns | 0.3690 ns | 0.3452 ns | 0.0025 |      40 B |
|             DisbledLogInformationWithParams | 28.865 ns | 0.5477 ns | 0.5123 ns | 0.0025 |      40 B |
|      EnabledLogWarningWithParamsCalculation | 45.114 ns | 0.8160 ns | 0.7234 ns | 0.0066 |     104 B |
| DisabledLogInformationWithParamsCalculation | 38.782 ns | 0.4981 ns | 0.4659 ns | 0.0066 |     104 B |
|      DisabledLogInformationWithEnabledCheck |  3.424 ns | 0.0341 ns | 0.0319 ns |      - |         - |
