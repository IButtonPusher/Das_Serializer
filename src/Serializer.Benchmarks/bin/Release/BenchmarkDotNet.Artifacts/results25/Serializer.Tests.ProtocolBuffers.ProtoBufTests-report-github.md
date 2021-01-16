``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   627.4 ns | 10.73 ns |  9.51 ns |
|          ProtoNetSimpleMessage | 1,147.1 ns |  7.08 ns |  6.62 ns |
|               DasDoubleMessage |   741.5 ns |  1.86 ns |  1.65 ns |
|         ProtoNetDoubleMeessage | 1,189.6 ns |  5.30 ns |  4.70 ns |
|               DasStringMessage |   819.0 ns |  2.89 ns |  2.41 ns |
|          ProtoNetStringMessage | 1,471.1 ns |  8.45 ns |  7.90 ns |
|                  DasDictionary | 5,540.8 ns | 17.76 ns | 16.61 ns |
|       ProtoNetObjectDictionary | 2,894.3 ns | 16.95 ns | 15.02 ns |
|      DasNegativeIntegerMessage |   695.2 ns |  6.28 ns |  5.88 ns |
| ProtoNetNegativeIntegerMessage | 1,171.6 ns |  5.87 ns |  5.49 ns |
|             DasMultiProperties | 1,076.2 ns | 16.05 ns | 15.01 ns |
|        ProtoNetMultiProperties | 1,530.1 ns |  9.05 ns |  8.02 ns |
|             DasComposedMessage | 6,713.2 ns | 38.74 ns | 34.35 ns |
|        ProtoNetComposedMessage | 3,307.1 ns | 25.44 ns | 23.80 ns |
|                   DasByteArray |   718.8 ns |  4.84 ns |  4.53 ns |
|              ProtoNetByteArray | 1,248.9 ns |  4.70 ns |  4.40 ns |
