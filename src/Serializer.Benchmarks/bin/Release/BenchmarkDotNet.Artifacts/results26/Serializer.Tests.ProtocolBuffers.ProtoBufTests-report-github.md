``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   631.7 ns |  8.88 ns |  8.31 ns |
|          ProtoNetSimpleMessage | 1,152.9 ns |  8.08 ns |  7.56 ns |
|               DasDoubleMessage |   724.4 ns |  3.93 ns |  3.48 ns |
|         ProtoNetDoubleMeessage | 1,202.9 ns |  9.70 ns |  9.08 ns |
|               DasStringMessage |   826.5 ns |  5.54 ns |  5.18 ns |
|          ProtoNetStringMessage | 1,477.5 ns | 13.20 ns | 12.35 ns |
|                  DasDictionary | 5,598.4 ns | 10.73 ns |  8.96 ns |
|       ProtoNetObjectDictionary | 2,865.3 ns |  9.92 ns |  8.29 ns |
|      DasNegativeIntegerMessage |   714.4 ns |  4.75 ns |  4.44 ns |
| ProtoNetNegativeIntegerMessage | 1,168.2 ns |  5.59 ns |  5.23 ns |
|             DasMultiProperties | 1,090.3 ns | 10.51 ns |  9.83 ns |
|        ProtoNetMultiProperties | 1,546.6 ns | 15.79 ns | 14.77 ns |
|             DasComposedMessage | 6,633.4 ns | 61.76 ns | 57.77 ns |
|        ProtoNetComposedMessage | 3,377.9 ns | 34.01 ns | 30.15 ns |
|                   DasByteArray |   709.1 ns |  7.68 ns |  7.18 ns |
|              ProtoNetByteArray | 1,247.8 ns |  6.85 ns |  6.41 ns |
