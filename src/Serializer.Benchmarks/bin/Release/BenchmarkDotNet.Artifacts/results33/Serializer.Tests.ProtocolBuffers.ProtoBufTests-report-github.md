``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   229.0 ns |  0.78 ns |  0.65 ns |
|          ProtoNetSimpleMessage | 1,159.7 ns |  4.55 ns |  4.03 ns |
|               DasDoubleMessage |   281.8 ns |  2.00 ns |  1.88 ns |
|         ProtoNetDoubleMeessage | 1,170.2 ns |  9.56 ns |  8.95 ns |
|               DasStringMessage |   417.8 ns |  2.91 ns |  2.72 ns |
|          ProtoNetStringMessage | 1,477.4 ns | 11.43 ns | 10.69 ns |
|                  DasDictionary | 5,582.0 ns | 41.77 ns | 39.07 ns |
|       ProtoNetObjectDictionary | 2,880.1 ns | 19.59 ns | 18.32 ns |
|      DasNegativeIntegerMessage |   642.0 ns |  5.41 ns |  5.06 ns |
| ProtoNetNegativeIntegerMessage | 1,171.3 ns |  6.95 ns |  6.51 ns |
|             DasMultiProperties |   956.5 ns | 11.99 ns | 11.22 ns |
|         DasMultiProperties_OLD | 1,098.6 ns |  7.57 ns |  7.08 ns |
|        ProtoNetMultiProperties | 1,566.8 ns | 12.87 ns | 12.04 ns |
|             DasComposedMessage | 5,086.9 ns | 33.93 ns | 31.74 ns |
|        ProtoNetComposedMessage | 3,194.4 ns | 12.26 ns | 11.47 ns |
|                   DasByteArray |   288.1 ns |  1.40 ns |  1.31 ns |
|              ProtoNetByteArray | 1,235.0 ns | 10.66 ns |  9.45 ns |
