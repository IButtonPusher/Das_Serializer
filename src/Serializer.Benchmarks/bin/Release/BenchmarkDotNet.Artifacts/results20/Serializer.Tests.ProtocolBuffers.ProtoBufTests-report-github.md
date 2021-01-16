``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   567.0 ns |  5.18 ns |  4.85 ns |
|          ProtoNetSimpleMessage | 1,153.8 ns |  7.01 ns |  6.56 ns |
|               DasDoubleMessage |   784.8 ns |  7.91 ns |  7.40 ns |
|         ProtoNetDoubleMeessage | 1,157.4 ns |  3.89 ns |  3.25 ns |
|               DasStringMessage |   768.6 ns |  3.94 ns |  3.69 ns |
|          ProtoNetStringMessage | 1,454.8 ns |  8.03 ns |  7.51 ns |
|                  DasDictionary | 6,040.7 ns | 28.97 ns | 27.10 ns |
|       ProtoNetObjectDictionary | 2,878.7 ns | 17.07 ns | 15.13 ns |
|      DasNegativeIntegerMessage |   618.3 ns |  6.35 ns |  5.94 ns |
| ProtoNetNegativeIntegerMessage | 1,173.5 ns | 10.58 ns |  9.90 ns |
|             DasMultiProperties | 1,026.6 ns |  7.54 ns |  7.06 ns |
|        ProtoNetMultiProperties | 1,560.9 ns | 13.65 ns | 12.77 ns |
|             DasComposedMessage | 1,809.5 ns |  8.76 ns |  8.19 ns |
|        ProtoNetComposedMessage | 1,796.3 ns | 15.10 ns | 14.13 ns |
|                   DasByteArray |   637.5 ns |  2.75 ns |  2.58 ns |
|              ProtoNetByteArray | 1,254.1 ns |  6.93 ns |  6.49 ns |
