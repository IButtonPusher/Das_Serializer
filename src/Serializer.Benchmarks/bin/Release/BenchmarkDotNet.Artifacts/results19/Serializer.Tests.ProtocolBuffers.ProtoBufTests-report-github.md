``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   595.4 ns |  3.70 ns |  3.28 ns |
|          ProtoNetSimpleMessage | 1,150.0 ns |  7.34 ns |  6.87 ns |
|               DasDoubleMessage |   775.3 ns |  3.53 ns |  3.30 ns |
|         ProtoNetDoubleMeessage | 1,182.5 ns |  9.76 ns |  9.13 ns |
|               DasStringMessage |   777.1 ns |  3.17 ns |  2.96 ns |
|          ProtoNetStringMessage | 1,492.2 ns |  4.63 ns |  4.33 ns |
|                  DasDictionary | 7,259.5 ns | 39.24 ns | 36.70 ns |
|       ProtoNetObjectDictionary | 2,850.0 ns | 26.34 ns | 24.63 ns |
|      DasNegativeIntegerMessage |   633.3 ns |  4.30 ns |  3.81 ns |
| ProtoNetNegativeIntegerMessage | 1,185.3 ns |  4.81 ns |  4.27 ns |
|             DasMultiProperties | 1,032.0 ns |  8.34 ns |  7.80 ns |
|        ProtoNetMultiProperties | 1,534.5 ns |  5.23 ns |  4.63 ns |
|             DasComposedMessage | 1,774.0 ns | 12.14 ns | 11.35 ns |
|        ProtoNetComposedMessage | 1,822.2 ns |  4.28 ns |  3.79 ns |
|                   DasByteArray |   652.6 ns | 10.27 ns |  9.10 ns |
|              ProtoNetByteArray | 1,267.4 ns |  4.65 ns |  4.35 ns |
