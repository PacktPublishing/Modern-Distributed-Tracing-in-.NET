``` ini

BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22621.1105)
12th Gen Intel Core i9-12900KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2


```
|                                      Method |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|-------------------------------------------- |----------:|----------:|----------:|-------:|----------:|
|                   EnabledLogWarningNoParams | 21.274 ns | 0.1217 ns | 0.1079 ns |      - |         - |
|              DisabledLogInformationNoParams | 14.053 ns | 0.0903 ns | 0.0845 ns |      - |         - |
|                 EnabledLogWarningWithParams | 32.858 ns | 0.3210 ns | 0.3002 ns | 0.0025 |      40 B |
|             DisbledLogInformationWithParams | 30.686 ns | 0.4553 ns | 0.4259 ns | 0.0025 |      40 B |
|      EnabledLogWarningWithParamsCalculation | 44.509 ns | 0.6509 ns | 0.6089 ns | 0.0066 |     104 B |
| DisabledLogInformationWithParamsCalculation | 37.737 ns | 0.7501 ns | 0.7017 ns | 0.0066 |     104 B |
|      DisabledLogInformationWithEnabledCheck |  3.421 ns | 0.0315 ns | 0.0295 ns |      - |         - |
