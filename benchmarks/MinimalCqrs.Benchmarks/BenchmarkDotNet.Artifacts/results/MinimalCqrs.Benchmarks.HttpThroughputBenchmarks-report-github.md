```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2


```
| Method                       | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Vanilla Minimal API — GET&#39;  |  5.649 μs | 0.1112 μs | 0.1559 μs |  5.605 μs |  1.00 |    0.04 | 0.4883 |   8.23 KB |        1.00 |
| &#39;MinimalCqrs — GET&#39;          |  5.943 μs | 0.1146 μs | 0.1126 μs |  5.942 μs |  1.05 |    0.03 | 0.4883 |   8.61 KB |        1.05 |
| &#39;Vanilla Minimal API — POST&#39; | 12.656 μs | 0.6015 μs | 1.7546 μs | 12.136 μs |  2.24 |    0.32 | 0.6104 |  10.97 KB |        1.33 |
| &#39;MinimalCqrs — POST&#39;         | 31.139 μs | 1.3055 μs | 3.6822 μs | 30.564 μs |  5.52 |    0.67 | 0.4883 |  11.34 KB |        1.38 |
