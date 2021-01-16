``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   566.7 ns |  5.00 ns |  4.68 ns |
|          ProtoNetSimpleMessage | 1,163.5 ns | 11.76 ns | 10.43 ns |
|               DasDoubleMessage |   660.6 ns |  1.46 ns |  1.30 ns |
|         ProtoNetDoubleMeessage | 1,190.1 ns |  5.41 ns |  4.52 ns |
|               DasStringMessage |   748.2 ns |  4.07 ns |  3.61 ns |
|          ProtoNetStringMessage | 1,472.9 ns |  9.58 ns |  8.00 ns |
|                  DasDictionary | 5,121.7 ns | 47.06 ns | 41.72 ns |
|       ProtoNetObjectDictionary | 2,904.3 ns | 19.40 ns | 18.14 ns |
|      DasNegativeIntegerMessage |   619.0 ns |  5.79 ns |  5.42 ns |
| ProtoNetNegativeIntegerMessage | 1,171.1 ns |  9.59 ns |  8.97 ns |
|             DasMultiProperties | 1,016.4 ns | 10.30 ns |  9.64 ns |
|        ProtoNetMultiProperties | 1,616.0 ns | 14.43 ns | 12.79 ns |
|             DasComposedMessage | 1,806.9 ns |  5.11 ns |  4.78 ns |
|        ProtoNetComposedMessage | 1,791.2 ns |  7.49 ns |  6.64 ns |
|                   DasByteArray |   645.0 ns |  2.70 ns |  2.53 ns |
|              ProtoNetByteArray | 1,240.5 ns |  4.65 ns |  4.12 ns |
