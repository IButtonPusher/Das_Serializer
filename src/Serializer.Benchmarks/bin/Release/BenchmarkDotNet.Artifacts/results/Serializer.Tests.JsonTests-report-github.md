``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.900 (1909/November2018Update/19H2)
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT


```
|                          Method |      Mean |     Error |    StdDev |
|-------------------------------- |----------:|----------:|----------:|
|   PrimitivePropertiesSubtractMe |  8.875 μs | 0.0422 μs | 0.0395 μs |
| PrimitivePropertiesJsonBaseline | 22.014 μs | 0.0970 μs | 0.0810 μs |
|  PrimitivePropertiesJsonExpress | 17.754 μs | 0.1347 μs | 0.1260 μs |
