``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   567.7 ns |  2.84 ns |  2.52 ns |
|          ProtoNetSimpleMessage | 1,156.8 ns |  9.08 ns |  8.05 ns |
|               DasDoubleMessage |   657.8 ns |  3.16 ns |  2.96 ns |
|         ProtoNetDoubleMeessage | 1,201.8 ns |  3.37 ns |  2.81 ns |
|               DasStringMessage |   756.3 ns |  3.82 ns |  3.19 ns |
|          ProtoNetStringMessage | 1,465.6 ns | 10.60 ns |  9.91 ns |
|                  DasDictionary | 5,268.9 ns | 12.72 ns |  9.93 ns |
|       ProtoNetObjectDictionary | 2,930.0 ns | 22.09 ns | 20.66 ns |
|      DasNegativeIntegerMessage |   614.0 ns |  3.61 ns |  3.38 ns |
| ProtoNetNegativeIntegerMessage | 1,179.6 ns |  8.48 ns |  7.93 ns |
|             DasMultiProperties | 1,050.5 ns |  6.55 ns |  5.81 ns |
|        ProtoNetMultiProperties | 1,527.4 ns |  3.77 ns |  3.53 ns |
|             DasComposedMessage | 1,758.2 ns |  9.83 ns |  9.20 ns |
|        ProtoNetComposedMessage | 1,818.4 ns | 22.93 ns | 21.45 ns |
|                   DasByteArray |   646.5 ns |  2.02 ns |  1.58 ns |
|              ProtoNetByteArray | 1,255.3 ns |  6.13 ns |  5.44 ns |
