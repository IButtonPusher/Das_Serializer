``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   566.8 ns |  6.01 ns |  5.62 ns |
|          ProtoNetSimpleMessage | 1,190.1 ns |  6.41 ns |  5.35 ns |
|               DasDoubleMessage |   764.1 ns |  4.97 ns |  4.65 ns |
|         ProtoNetDoubleMeessage | 1,164.3 ns |  6.53 ns |  6.11 ns |
|               DasStringMessage |   765.0 ns |  3.40 ns |  2.84 ns |
|          ProtoNetStringMessage | 1,447.1 ns |  5.74 ns |  5.09 ns |
|                  DasDictionary | 6,185.0 ns | 60.84 ns | 56.91 ns |
|       ProtoNetObjectDictionary | 2,882.4 ns | 10.00 ns |  9.36 ns |
|      DasNegativeIntegerMessage |   615.3 ns |  2.18 ns |  1.82 ns |
| ProtoNetNegativeIntegerMessage | 1,165.7 ns |  3.21 ns |  2.51 ns |
|             DasMultiProperties | 1,019.9 ns |  7.47 ns |  6.62 ns |
|        ProtoNetMultiProperties | 1,537.7 ns | 22.15 ns | 20.72 ns |
|             DasComposedMessage | 1,790.4 ns | 10.55 ns |  9.87 ns |
|        ProtoNetComposedMessage | 1,894.4 ns | 12.92 ns | 11.45 ns |
|                   DasByteArray |   647.6 ns |  5.51 ns |  4.89 ns |
|              ProtoNetByteArray | 1,236.0 ns |  5.70 ns |  5.33 ns |
