``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4042.0
  DefaultJob : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4042.0


```
|                Method |       Mean |    Error |   StdDev |
|---------------------- |-----------:|---------:|---------:|
|      DasSimpleMessage |   864.4 ns | 7.251 ns | 6.428 ns |
| ProtoNetSimpleMessage | 1,164.5 ns | 6.293 ns | 5.886 ns |
