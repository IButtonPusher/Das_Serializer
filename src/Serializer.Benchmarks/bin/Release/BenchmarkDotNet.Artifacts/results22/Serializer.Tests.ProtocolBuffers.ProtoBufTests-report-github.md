``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   583.8 ns |  8.75 ns |  8.19 ns |
|          ProtoNetSimpleMessage | 1,182.9 ns |  3.42 ns |  3.03 ns |
|               DasDoubleMessage |   661.2 ns |  3.53 ns |  3.30 ns |
|         ProtoNetDoubleMeessage | 1,173.9 ns |  7.70 ns |  7.20 ns |
|               DasStringMessage |   744.0 ns |  6.38 ns |  5.96 ns |
|          ProtoNetStringMessage | 1,488.2 ns |  3.72 ns |  3.48 ns |
|                  DasDictionary | 5,208.2 ns | 39.90 ns | 35.37 ns |
|       ProtoNetObjectDictionary | 2,848.7 ns | 14.85 ns | 13.89 ns |
|      DasNegativeIntegerMessage |   617.9 ns |  2.48 ns |  2.32 ns |
| ProtoNetNegativeIntegerMessage | 1,174.2 ns |  4.80 ns |  4.49 ns |
|             DasMultiProperties | 1,056.3 ns |  3.83 ns |  3.40 ns |
|        ProtoNetMultiProperties | 1,546.7 ns |  3.80 ns |  3.37 ns |
|             DasComposedMessage | 1,771.4 ns | 10.80 ns | 10.10 ns |
|        ProtoNetComposedMessage | 1,918.8 ns | 21.65 ns | 20.25 ns |
|                   DasByteArray |   678.8 ns |  4.76 ns |  4.22 ns |
|              ProtoNetByteArray | 1,276.1 ns | 17.89 ns | 16.73 ns |
