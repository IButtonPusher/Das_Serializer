``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                  Method |       Mean |    Error |   StdDev |
|------------------------ |-----------:|---------:|---------:|
|        DasSimpleMessage |   578.7 ns |  7.35 ns |  6.51 ns |
|   ProtoNetSimpleMessage | 1,146.9 ns |  6.20 ns |  5.49 ns |
|       DasDoubleMeessage |   793.7 ns |  3.85 ns |  3.42 ns |
|  ProtoNetDoubleMeessage | 1,160.1 ns |  5.47 ns |  4.85 ns |
|        DasStringMessage |   783.3 ns |  1.59 ns |  1.41 ns |
|   ProtoNetStringMessage | 1,449.4 ns |  5.41 ns |  5.06 ns |
|      DasMultiProperties | 1,040.4 ns |  7.65 ns |  7.15 ns |
| ProtoNetMultiProperties | 1,513.9 ns |  8.26 ns |  7.72 ns |
|      DasComposedMessage | 1,883.4 ns |  6.17 ns |  5.77 ns |
| ProtoNetComposedMessage | 1,827.2 ns | 16.38 ns | 15.33 ns |
|            DasByteArray |   658.1 ns |  3.44 ns |  3.22 ns |
|       ProtoNetByteArray | 1,235.5 ns |  5.08 ns |  4.75 ns |
