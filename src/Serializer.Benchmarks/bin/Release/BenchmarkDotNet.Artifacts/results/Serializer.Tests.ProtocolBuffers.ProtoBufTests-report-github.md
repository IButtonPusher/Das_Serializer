``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.900 (1909/November2018Update/19H2)
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT


```
|                            Method |       Mean |    Error |   StdDev |
|---------------------------------- |-----------:|---------:|---------:|
|                  DasSimpleMessage |   129.3 ns |  0.99 ns |  0.88 ns |
|             ProtoNetSimpleMessage |   763.5 ns |  2.49 ns |  2.21 ns |
|                  DasDoubleMessage |   179.9 ns |  0.91 ns |  0.81 ns |
|            ProtoNetDoubleMeessage |   789.9 ns |  4.32 ns |  3.60 ns |
|                  DasStringMessage |   290.0 ns |  1.92 ns |  1.70 ns |
|             ProtoNetStringMessage |   880.5 ns |  3.62 ns |  3.39 ns |
|                     DasDictionary | 2,279.1 ns |  7.83 ns |  7.33 ns |
|          ProtoNetObjectDictionary | 4,248.8 ns | 22.83 ns | 19.07 ns |
|                    DasCollections | 2,049.5 ns |  4.12 ns |  3.65 ns |
|                  ProtoCollections | 3,577.4 ns | 10.55 ns |  8.24 ns |
|                    DasPackedArray |   385.2 ns |  0.83 ns |  0.69 ns |
|                  ProtoPackedArray | 1,217.4 ns |  2.76 ns |  2.31 ns |
|         DasNegativeIntegerMessage |   178.0 ns |  0.85 ns |  0.79 ns |
|    ProtoNetNegativeIntegerMessage |   799.5 ns |  2.91 ns |  2.43 ns |
|                DasMultiProperties |   331.7 ns |  2.32 ns |  1.94 ns |
|           ProtoNetMultiProperties |   929.2 ns |  4.87 ns |  4.56 ns |
|                DasComposedMessage | 1,614.8 ns |  2.99 ns |  2.50 ns |
|           ProtoNetComposedMessage | 3,156.6 ns |  6.79 ns |  6.02 ns |
|      DasComposedCollectionMessage | 3,276.3 ns | 14.26 ns | 13.34 ns |
| ProtoNetComposedCollectionMessage | 6,541.4 ns | 33.40 ns | 29.61 ns |
|                      DasByteArray |   158.0 ns |  0.59 ns |  0.52 ns |
|                 ProtoNetByteArray |   931.5 ns |  2.50 ns |  2.09 ns |
