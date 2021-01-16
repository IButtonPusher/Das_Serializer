``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   246.5 ns |  4.89 ns |  5.23 ns |
|          ProtoNetSimpleMessage | 1,199.4 ns | 25.66 ns | 39.95 ns |
|               DasDoubleMessage |   286.6 ns |  5.58 ns |  5.22 ns |
|         ProtoNetDoubleMeessage | 1,181.5 ns |  7.88 ns |  7.37 ns |
|               DasStringMessage |   416.4 ns |  3.82 ns |  3.39 ns |
|          ProtoNetStringMessage | 1,493.8 ns | 12.20 ns | 11.41 ns |
|                  DasDictionary | 1,997.0 ns | 31.01 ns | 29.01 ns |
|       ProtoNetObjectDictionary | 2,850.3 ns | 30.27 ns | 28.31 ns |
|      DasNegativeIntegerMessage |   302.8 ns |  1.73 ns |  1.54 ns |
| ProtoNetNegativeIntegerMessage | 1,202.0 ns | 22.53 ns | 21.08 ns |
|             DasMultiProperties |   480.4 ns |  9.25 ns | 10.65 ns |
|        ProtoNetMultiProperties | 1,564.0 ns | 29.37 ns | 26.03 ns |
|             DasComposedMessage | 1,856.3 ns | 29.61 ns | 27.70 ns |
|        ProtoNetComposedMessage | 3,284.7 ns | 19.24 ns | 17.99 ns |
|                   DasByteArray |   288.4 ns |  3.50 ns |  3.28 ns |
|              ProtoNetByteArray | 1,227.4 ns | 18.78 ns | 17.57 ns |
