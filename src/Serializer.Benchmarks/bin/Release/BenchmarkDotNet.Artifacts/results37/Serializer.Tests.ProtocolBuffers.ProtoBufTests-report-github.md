``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   237.1 ns |  2.95 ns |  2.61 ns |
|          ProtoNetSimpleMessage | 1,162.8 ns |  7.58 ns |  7.09 ns |
|               DasDoubleMessage |   291.3 ns |  2.30 ns |  2.15 ns |
|         ProtoNetDoubleMeessage | 1,177.8 ns |  7.28 ns |  6.81 ns |
|               DasStringMessage |   419.1 ns |  2.70 ns |  2.53 ns |
|          ProtoNetStringMessage | 1,465.4 ns | 10.98 ns | 10.27 ns |
|                  DasDictionary | 3,634.1 ns | 19.05 ns | 17.82 ns |
|       ProtoNetObjectDictionary | 2,944.9 ns | 25.64 ns | 23.99 ns |
|      DasNegativeIntegerMessage |   317.0 ns |  3.96 ns |  3.51 ns |
| ProtoNetNegativeIntegerMessage | 1,160.2 ns |  4.54 ns |  4.02 ns |
|             DasMultiProperties |   463.2 ns |  2.78 ns |  2.47 ns |
|        ProtoNetMultiProperties | 1,533.8 ns | 12.94 ns | 11.47 ns |
|             DasComposedMessage | 1,947.0 ns | 21.59 ns | 20.20 ns |
|        ProtoNetComposedMessage | 3,322.9 ns | 64.38 ns | 74.14 ns |
|                   DasByteArray |   296.2 ns |  3.29 ns |  2.91 ns |
|              ProtoNetByteArray | 1,235.1 ns |  8.59 ns |  8.04 ns |
