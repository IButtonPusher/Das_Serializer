``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4042.0
  DefaultJob : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4042.0


```
|                Method |       Mean |    Error |   StdDev |
|---------------------- |-----------:|---------:|---------:|
|      DasSimpleMessage |   864.1 ns | 6.905 ns | 5.766 ns |
| ProtoNetSimpleMessage | 1,153.7 ns | 5.883 ns | 5.503 ns |
