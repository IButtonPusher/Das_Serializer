``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   233.9 ns |  2.09 ns |  1.96 ns |
|          ProtoNetSimpleMessage | 1,168.9 ns |  4.20 ns |  3.51 ns |
|               DasDoubleMessage |   270.1 ns |  1.53 ns |  1.27 ns |
|         ProtoNetDoubleMeessage | 1,230.1 ns |  9.96 ns |  9.31 ns |
|               DasStringMessage |   400.4 ns |  2.75 ns |  2.29 ns |
|          ProtoNetStringMessage | 1,469.1 ns |  8.32 ns |  7.79 ns |
|                  DasDictionary | 1,947.6 ns |  5.61 ns |  4.69 ns |
|       ProtoNetObjectDictionary | 2,836.0 ns | 11.49 ns | 10.75 ns |
|      DasNegativeIntegerMessage |   290.6 ns |  4.54 ns |  4.24 ns |
| ProtoNetNegativeIntegerMessage | 1,287.8 ns |  6.28 ns |  5.88 ns |
|             DasMultiProperties |   450.1 ns |  4.83 ns |  4.52 ns |
|        ProtoNetMultiProperties | 1,597.6 ns |  9.80 ns |  9.16 ns |
|             DasComposedMessage | 1,995.1 ns | 13.02 ns | 11.54 ns |
|        ProtoNetComposedMessage | 3,177.0 ns | 18.26 ns | 16.19 ns |
|                   DasByteArray |   279.0 ns |  1.07 ns |  0.95 ns |
|              ProtoNetByteArray | 1,219.1 ns |  3.94 ns |  3.49 ns |
