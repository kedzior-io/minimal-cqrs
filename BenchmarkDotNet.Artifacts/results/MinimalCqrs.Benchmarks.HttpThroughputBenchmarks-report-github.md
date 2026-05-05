```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2


```
| Method                       | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Vanilla Minimal API — GET&#39;  |  5.943 μs | 0.1387 μs | 0.3979 μs |  5.804 μs |  1.00 |    0.09 | 0.4883 |   8.23 KB |        1.00 |
| &#39;MinimalCqrs — GET&#39;          |  6.113 μs | 0.1043 μs | 0.1799 μs |  6.061 μs |  1.03 |    0.07 | 0.4883 |   8.61 KB |        1.05 |
| &#39;Vanilla Minimal API — POST&#39; |  9.597 μs | 0.1859 μs | 0.2482 μs |  9.530 μs |  1.62 |    0.11 | 0.6104 |  10.94 KB |        1.33 |
| &#39;MinimalCqrs — POST&#39;         | 14.635 μs | 0.6318 μs | 1.8329 μs | 14.577 μs |  2.47 |    0.35 | 0.6104 |   11.3 KB |        1.37 |
