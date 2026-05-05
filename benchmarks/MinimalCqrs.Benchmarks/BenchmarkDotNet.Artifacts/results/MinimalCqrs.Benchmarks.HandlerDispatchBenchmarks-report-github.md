```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2


```
| Method                                                        | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------------------------------- |---------:|--------:|--------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;No deps — no HTTP context&#39;                                   | 119.4 μs | 1.26 μs | 1.17 μs |  1.00 |    0.01 | 0.2441 |      - |   6.31 KB |        1.00 |
| &#39;Scoped dep — no HTTP context&#39;                                | 172.2 μs | 2.17 μs | 1.92 μs |  1.44 |    0.02 | 0.2441 |      - |   7.42 KB |        1.17 |
| &#39;Transient+IDisposable dep — no HTTP context&#39;                 | 172.2 μs | 2.08 μs | 1.84 μs |  1.44 |    0.02 | 0.2441 |      - |   7.33 KB |        1.16 |
| &#39;Mixed deps (Scoped+Transient+Singleton) — no HTTP context&#39;   | 231.1 μs | 3.76 μs | 3.52 μs |  1.94 |    0.03 | 0.4883 |      - |   9.28 KB |        1.47 |
| &#39;No deps — with HTTP context&#39;                                 | 121.5 μs | 2.39 μs | 2.12 μs |  1.02 |    0.02 | 0.2441 |      - |   7.46 KB |        1.18 |
| &#39;Scoped dep — with HTTP context&#39;                              | 176.4 μs | 2.01 μs | 1.88 μs |  1.48 |    0.02 | 0.4883 | 0.2441 |   8.58 KB |        1.36 |
| &#39;Mixed deps (Scoped+Transient+Singleton) — with HTTP context&#39; | 226.7 μs | 1.82 μs | 1.70 μs |  1.90 |    0.02 | 0.4883 |      - |  10.61 KB |        1.68 |
