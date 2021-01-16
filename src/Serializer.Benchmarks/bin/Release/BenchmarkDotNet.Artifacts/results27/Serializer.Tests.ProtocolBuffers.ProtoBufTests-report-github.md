``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   569.1 ns | 11.14 ns | 12.39 ns |
|          ProtoNetSimpleMessage | 1,176.8 ns | 12.52 ns | 11.10 ns |
|               DasDoubleMessage |   742.8 ns |  5.84 ns |  5.47 ns |
|         ProtoNetDoubleMeessage | 1,181.3 ns | 15.22 ns | 13.50 ns |
|               DasStringMessage |   863.1 ns | 19.67 ns | 35.96 ns |
|          ProtoNetStringMessage | 1,486.1 ns |  8.00 ns |  7.09 ns |
|                  DasDictionary | 5,691.9 ns | 55.09 ns | 48.84 ns |
|       ProtoNetObjectDictionary | 2,894.7 ns | 11.15 ns |  9.89 ns |
|      DasNegativeIntegerMessage |   726.5 ns | 10.70 ns | 10.01 ns |
| ProtoNetNegativeIntegerMessage | 1,190.8 ns |  7.41 ns |  6.19 ns |
|             DasMultiProperties | 1,168.7 ns | 24.23 ns | 59.88 ns |
|        ProtoNetMultiProperties | 1,555.6 ns |  9.72 ns |  8.61 ns |
|             DasComposedMessage | 6,644.9 ns | 66.35 ns | 58.82 ns |
|        ProtoNetComposedMessage | 3,228.8 ns | 14.81 ns | 13.13 ns |
|                   DasByteArray |   734.1 ns | 23.56 ns | 19.67 ns |
|              ProtoNetByteArray | 1,265.9 ns | 23.69 ns | 21.00 ns |
