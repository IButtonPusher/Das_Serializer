``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   239.7 ns |  2.67 ns |  2.50 ns |
|          ProtoNetSimpleMessage | 1,250.6 ns | 24.50 ns | 38.86 ns |
|               DasDoubleMessage |   287.7 ns |  1.88 ns |  1.76 ns |
|         ProtoNetDoubleMeessage | 1,231.0 ns | 24.14 ns | 35.39 ns |
|               DasStringMessage |   770.1 ns |  4.36 ns |  3.87 ns |
|          ProtoNetStringMessage | 1,477.5 ns |  5.58 ns |  4.94 ns |
|                  DasDictionary | 5,748.7 ns | 29.91 ns | 27.98 ns |
|       ProtoNetObjectDictionary | 2,944.7 ns |  7.42 ns |  6.94 ns |
|      DasNegativeIntegerMessage |   629.9 ns |  4.30 ns |  4.02 ns |
| ProtoNetNegativeIntegerMessage | 1,213.5 ns |  6.91 ns |  6.47 ns |
|             DasMultiProperties |   941.4 ns |  4.48 ns |  3.97 ns |
|         DasMultiProperties_OLD | 1,093.6 ns |  6.97 ns |  6.18 ns |
|        ProtoNetMultiProperties | 1,535.3 ns |  8.32 ns |  7.79 ns |
|             DasComposedMessage | 5,264.2 ns | 30.75 ns | 27.26 ns |
|        ProtoNetComposedMessage | 3,253.3 ns | 16.61 ns | 14.73 ns |
|                   DasByteArray |   634.2 ns |  3.30 ns |  2.93 ns |
|              ProtoNetByteArray | 1,263.5 ns |  7.14 ns |  6.68 ns |
