``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   256.0 ns |  2.58 ns |  2.29 ns |
|          ProtoNetSimpleMessage | 1,171.1 ns |  8.77 ns |  8.20 ns |
|               DasDoubleMessage |   288.0 ns |  2.28 ns |  2.13 ns |
|         ProtoNetDoubleMeessage | 1,185.7 ns |  4.71 ns |  4.40 ns |
|               DasStringMessage |   418.9 ns |  4.40 ns |  4.11 ns |
|          ProtoNetStringMessage | 1,480.2 ns |  9.46 ns |  8.85 ns |
|                  DasDictionary | 2,049.8 ns | 12.14 ns | 10.76 ns |
|       ProtoNetObjectDictionary | 2,900.4 ns | 18.91 ns | 16.76 ns |
|      DasNegativeIntegerMessage |   299.8 ns |  1.98 ns |  1.85 ns |
| ProtoNetNegativeIntegerMessage | 1,210.9 ns | 11.20 ns | 10.47 ns |
|             DasMultiProperties |   465.3 ns |  3.17 ns |  2.97 ns |
|        ProtoNetMultiProperties | 1,516.9 ns |  7.48 ns |  6.63 ns |
|             DasComposedMessage | 1,965.8 ns | 11.31 ns | 10.03 ns |
|        ProtoNetComposedMessage | 3,205.9 ns | 15.74 ns | 14.72 ns |
|                   DasByteArray |   311.5 ns |  1.54 ns |  1.44 ns |
|              ProtoNetByteArray | 1,242.8 ns |  6.17 ns |  5.47 ns |
