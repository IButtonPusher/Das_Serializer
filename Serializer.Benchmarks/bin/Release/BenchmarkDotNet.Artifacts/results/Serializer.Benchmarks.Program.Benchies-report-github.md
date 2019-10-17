``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4010.0
  DefaultJob : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4010.0


```
|                Method |     Mean |     Error |    StdDev |
|---------------------- |---------:|----------:|----------:|
|  DeserializeCharArray | 152.0 us | 0.8287 us | 0.7752 us |
| DeserializeEnumerable | 152.3 us | 1.1099 us | 0.9839 us |
