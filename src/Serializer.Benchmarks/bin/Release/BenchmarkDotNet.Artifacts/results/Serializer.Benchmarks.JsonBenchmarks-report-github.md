``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.900 (1909/November2018Update/19H2)
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT


```
|                          Method |     Mean |     Error |    StdDev |
|-------------------------------- |---------:|----------:|----------:|
| PrimitivePropertiesJsonBaseline | 8.836 μs | 0.0601 μs | 0.0562 μs |
|  PrimitivePropertiesJsonExpress | 5.988 μs | 0.0338 μs | 0.0316 μs |
