``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |    StdDev |     Median |
|------------------------------- |-----------:|---------:|----------:|-----------:|
|               DasSimpleMessage |   253.2 ns |  4.80 ns |   4.26 ns |   252.6 ns |
|          ProtoNetSimpleMessage | 1,287.9 ns | 38.41 ns | 112.04 ns | 1,243.6 ns |
|               DasDoubleMessage |   285.6 ns |  0.90 ns |   0.70 ns |   285.7 ns |
|         ProtoNetDoubleMeessage | 1,196.4 ns |  9.77 ns |   8.16 ns | 1,198.7 ns |
|               DasStringMessage |   434.5 ns |  8.06 ns |   7.91 ns |   432.1 ns |
|          ProtoNetStringMessage | 1,488.4 ns |  5.96 ns |   5.28 ns | 1,488.5 ns |
|                  DasDictionary | 1,999.4 ns | 11.62 ns |   9.70 ns | 1,998.1 ns |
|       ProtoNetObjectDictionary | 2,896.5 ns | 30.96 ns |  27.45 ns | 2,898.9 ns |
|      DasNegativeIntegerMessage |   316.1 ns |  6.24 ns |  11.42 ns |   313.7 ns |
| ProtoNetNegativeIntegerMessage | 1,237.5 ns | 28.34 ns |  29.10 ns | 1,238.8 ns |
|             DasMultiProperties |   466.6 ns |  4.81 ns |   3.76 ns |   467.1 ns |
|        ProtoNetMultiProperties | 1,557.8 ns | 10.86 ns |   9.07 ns | 1,557.8 ns |
|             DasComposedMessage | 1,916.6 ns | 12.87 ns |  11.41 ns | 1,917.2 ns |
|        ProtoNetComposedMessage | 3,278.5 ns | 18.01 ns |  15.96 ns | 3,271.1 ns |
|                   DasByteArray |   300.6 ns |  6.74 ns |   6.31 ns |   299.4 ns |
|              ProtoNetByteArray | 1,247.3 ns |  7.88 ns |   6.99 ns | 1,246.9 ns |
