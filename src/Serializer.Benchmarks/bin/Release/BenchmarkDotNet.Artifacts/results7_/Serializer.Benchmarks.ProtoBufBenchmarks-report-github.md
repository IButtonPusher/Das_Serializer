``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4042.0
  DefaultJob : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4042.0


```
|                Method |     Mean |     Error |    StdDev |
|---------------------- |---------:|----------:|----------:|
|      DasSimpleMessage | 2.144 us | 0.0122 us | 0.0114 us |
| ProtoNetSimpleMessage | 1.173 us | 0.0104 us | 0.0092 us |
