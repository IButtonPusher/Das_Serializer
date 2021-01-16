``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.836 (1909/November2018Update/19H2)
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   290.0 ns |  2.92 ns |  2.59 ns |
|          ProtoNetSimpleMessage |   831.8 ns |  8.41 ns |  7.45 ns |
|               DasDoubleMessage |   342.5 ns |  5.02 ns |  4.69 ns |
|         ProtoNetDoubleMeessage |   889.7 ns | 17.01 ns | 15.91 ns |
|               DasStringMessage |   465.6 ns |  2.26 ns |  2.11 ns |
|          ProtoNetStringMessage |   966.5 ns |  7.55 ns |  7.06 ns |
|                  DasDictionary | 2,175.3 ns | 35.75 ns | 31.69 ns |
|       ProtoNetObjectDictionary | 4,443.5 ns | 21.24 ns | 17.74 ns |
|                 DasCollections | 1,107.9 ns | 12.89 ns | 12.06 ns |
|               ProtoCollections | 2,298.4 ns | 13.28 ns | 11.09 ns |
|               ProtoPackedArray | 1,333.1 ns |  9.23 ns |  8.63 ns |
|                 DasPackedArray |   605.3 ns |  3.17 ns |  2.97 ns |
|      DasNegativeIntegerMessage |   349.6 ns |  4.13 ns |  3.86 ns |
| ProtoNetNegativeIntegerMessage |   865.8 ns | 11.06 ns |  9.24 ns |
|             DasMultiProperties |   517.7 ns |  3.38 ns |  3.16 ns |
|        ProtoNetMultiProperties | 1,000.7 ns |  3.10 ns |  2.59 ns |
|             DasComposedMessage | 1,982.0 ns | 25.92 ns | 24.24 ns |
|        ProtoNetComposedMessage | 3,370.8 ns | 48.56 ns | 45.42 ns |
|                   DasByteArray |   347.1 ns |  2.10 ns |  1.96 ns |
|              ProtoNetByteArray |   991.5 ns |  3.35 ns |  2.97 ns |
