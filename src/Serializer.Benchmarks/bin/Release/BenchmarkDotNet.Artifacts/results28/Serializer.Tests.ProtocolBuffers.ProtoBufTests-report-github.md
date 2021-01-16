``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |     Median |
|------------------------------- |-----------:|---------:|---------:|-----------:|
|               DasSimpleMessage |   602.0 ns | 11.99 ns | 29.86 ns |   591.8 ns |
|          ProtoNetSimpleMessage | 1,189.5 ns | 23.34 ns | 33.47 ns | 1,170.4 ns |
|               DasDoubleMessage |   737.9 ns | 12.70 ns | 10.60 ns |   738.6 ns |
|         ProtoNetDoubleMeessage | 1,184.9 ns | 11.87 ns | 10.52 ns | 1,184.5 ns |
|               DasStringMessage |   830.8 ns |  2.56 ns |  2.39 ns |   830.5 ns |
|          ProtoNetStringMessage | 1,498.2 ns | 29.63 ns | 38.53 ns | 1,478.7 ns |
|                  DasDictionary | 5,779.6 ns | 44.24 ns | 41.38 ns | 5,767.8 ns |
|       ProtoNetObjectDictionary | 2,957.7 ns | 32.36 ns | 28.69 ns | 2,957.7 ns |
|      DasNegativeIntegerMessage |   706.0 ns |  7.84 ns |  6.95 ns |   707.7 ns |
| ProtoNetNegativeIntegerMessage | 1,188.0 ns |  8.49 ns |  7.09 ns | 1,186.9 ns |
|             DasMultiProperties | 1,101.8 ns | 17.73 ns | 14.81 ns | 1,097.0 ns |
|        ProtoNetMultiProperties | 1,540.5 ns | 11.72 ns | 10.96 ns | 1,543.0 ns |
|             DasComposedMessage | 6,543.9 ns | 32.11 ns | 28.46 ns | 6,538.6 ns |
|        ProtoNetComposedMessage | 3,234.9 ns | 14.06 ns | 13.15 ns | 3,235.0 ns |
|                   DasByteArray |   706.4 ns | 16.90 ns | 19.46 ns |   697.5 ns |
|              ProtoNetByteArray | 1,284.2 ns | 27.98 ns | 39.23 ns | 1,266.4 ns |
