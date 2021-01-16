``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   232.1 ns |  1.79 ns |  1.67 ns |
|          ProtoNetSimpleMessage | 1,138.1 ns |  3.59 ns |  3.36 ns |
|               DasDoubleMessage |   290.2 ns |  0.72 ns |  0.64 ns |
|         ProtoNetDoubleMeessage | 1,150.7 ns |  6.96 ns |  6.51 ns |
|               DasStringMessage |   421.2 ns |  7.61 ns |  7.12 ns |
|          ProtoNetStringMessage | 1,457.8 ns |  4.67 ns |  4.14 ns |
|                  DasDictionary | 5,529.9 ns | 13.57 ns | 11.33 ns |
|       ProtoNetObjectDictionary | 2,816.2 ns |  5.15 ns |  4.30 ns |
|      DasNegativeIntegerMessage |   286.9 ns |  1.24 ns |  1.16 ns |
| ProtoNetNegativeIntegerMessage | 1,165.6 ns |  4.06 ns |  3.79 ns |
|             DasMultiProperties |   952.4 ns |  3.66 ns |  3.42 ns |
|         DasMultiProperties_OLD | 1,085.4 ns |  8.75 ns |  8.18 ns |
|        ProtoNetMultiProperties | 1,547.8 ns |  2.89 ns |  2.41 ns |
|             DasComposedMessage | 5,018.9 ns | 25.30 ns | 23.66 ns |
|        ProtoNetComposedMessage | 3,157.6 ns |  5.74 ns |  4.79 ns |
|                   DasByteArray |   297.0 ns |  0.87 ns |  0.82 ns |
|              ProtoNetByteArray | 1,247.5 ns |  7.15 ns |  6.69 ns |
