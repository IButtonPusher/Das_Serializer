``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |     Error |    StdDev |
|------------------------------- |-----------:|----------:|----------:|
|               DasSimpleMessage |   229.7 ns |   1.53 ns |   1.43 ns |
|          ProtoNetSimpleMessage | 1,185.0 ns |  17.43 ns |  15.45 ns |
|               DasDoubleMessage |   276.6 ns |   3.75 ns |   3.13 ns |
|         ProtoNetDoubleMeessage | 1,180.2 ns |  10.90 ns |   9.66 ns |
|               DasStringMessage |   429.1 ns |   8.34 ns |  11.42 ns |
|          ProtoNetStringMessage | 1,464.8 ns |  14.30 ns |  12.68 ns |
|                  DasDictionary | 5,779.0 ns | 111.21 ns | 136.57 ns |
|       ProtoNetObjectDictionary | 2,911.0 ns |  13.93 ns |  13.03 ns |
|      DasNegativeIntegerMessage |   297.3 ns |   3.59 ns |   3.35 ns |
| ProtoNetNegativeIntegerMessage | 1,167.7 ns |   5.54 ns |   5.18 ns |
|             DasMultiProperties |   467.1 ns |   7.52 ns |   7.03 ns |
|         DasMultiProperties_OLD | 1,103.8 ns |  15.14 ns |  12.65 ns |
|        ProtoNetMultiProperties | 1,544.6 ns |   5.80 ns |   5.14 ns |
|             DasComposedMessage | 5,319.7 ns | 104.85 ns | 124.81 ns |
|        ProtoNetComposedMessage | 3,229.1 ns |  20.12 ns |  18.82 ns |
|                   DasByteArray |   290.9 ns |   1.30 ns |   1.21 ns |
|              ProtoNetByteArray | 1,244.1 ns |   8.71 ns |   7.72 ns |
