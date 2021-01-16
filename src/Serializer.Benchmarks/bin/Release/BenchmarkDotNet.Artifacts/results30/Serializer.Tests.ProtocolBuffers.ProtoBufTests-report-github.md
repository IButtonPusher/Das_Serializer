``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   203.8 ns |  1.57 ns |  1.31 ns |
|          ProtoNetSimpleMessage | 1,209.8 ns |  9.81 ns |  8.19 ns |
|               DasDoubleMessage |   658.1 ns |  5.75 ns |  5.38 ns |
|         ProtoNetDoubleMeessage | 1,182.2 ns | 10.81 ns | 10.11 ns |
|               DasStringMessage |   761.6 ns |  1.85 ns |  1.64 ns |
|          ProtoNetStringMessage | 1,483.3 ns |  7.71 ns |  7.21 ns |
|                  DasDictionary | 5,720.1 ns | 42.68 ns | 37.84 ns |
|       ProtoNetObjectDictionary | 2,900.1 ns | 24.95 ns | 23.34 ns |
|      DasNegativeIntegerMessage |   631.3 ns |  4.65 ns |  4.35 ns |
| ProtoNetNegativeIntegerMessage | 1,183.6 ns | 11.44 ns | 10.14 ns |
|             DasMultiProperties |   954.2 ns |  8.68 ns |  8.12 ns |
|         DasMultiProperties_OLD | 1,128.2 ns | 15.10 ns | 14.12 ns |
|        ProtoNetMultiProperties | 1,532.9 ns |  9.75 ns |  9.12 ns |
|             DasComposedMessage | 5,217.7 ns | 32.80 ns | 29.07 ns |
|        ProtoNetComposedMessage | 3,209.0 ns | 11.85 ns | 10.51 ns |
|                   DasByteArray |   650.2 ns |  5.01 ns |  4.69 ns |
|              ProtoNetByteArray | 1,248.3 ns |  4.98 ns |  4.16 ns |
