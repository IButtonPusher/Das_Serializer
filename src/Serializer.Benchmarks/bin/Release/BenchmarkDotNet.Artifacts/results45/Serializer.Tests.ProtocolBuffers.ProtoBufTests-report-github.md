``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.900 (1909/November2018Update/19H2)
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4180.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   134.3 ns |  0.36 ns |  0.30 ns |
|          ProtoNetSimpleMessage |   765.2 ns |  3.22 ns |  2.85 ns |
|               DasDoubleMessage |   178.7 ns |  0.58 ns |  0.54 ns |
|         ProtoNetDoubleMeessage |   787.0 ns |  3.94 ns |  3.29 ns |
|               DasStringMessage |   290.8 ns |  1.60 ns |  1.42 ns |
|          ProtoNetStringMessage |   909.3 ns |  5.79 ns |  5.42 ns |
|                  DasDictionary | 2,247.6 ns | 22.28 ns | 19.75 ns |
|       ProtoNetObjectDictionary | 4,143.3 ns | 29.60 ns | 26.24 ns |
|                 DasCollections | 2,057.9 ns | 18.39 ns | 16.30 ns |
|               ProtoCollections | 3,618.9 ns | 40.04 ns | 31.26 ns |
|                 DasPackedArray |   393.5 ns |  4.01 ns |  3.35 ns |
|               ProtoPackedArray | 1,198.7 ns |  4.67 ns |  3.90 ns |
|      DasNegativeIntegerMessage |   179.1 ns |  1.34 ns |  1.19 ns |
| ProtoNetNegativeIntegerMessage |   797.9 ns | 12.92 ns | 11.45 ns |
|             DasMultiProperties |   338.9 ns |  2.31 ns |  2.16 ns |
|        ProtoNetMultiProperties | 1,011.7 ns |  7.51 ns |  7.02 ns |
|             DasComposedMessage | 1,634.7 ns | 31.15 ns | 83.13 ns |
|        ProtoNetComposedMessage | 3,231.7 ns | 32.46 ns | 28.78 ns |
|                   DasByteArray |   170.6 ns |  1.49 ns |  1.40 ns |
|              ProtoNetByteArray |   945.6 ns | 18.04 ns | 20.77 ns |
