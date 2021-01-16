``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |    StdDev |     Median |
|------------------------------- |-----------:|---------:|----------:|-----------:|
|               DasSimpleMessage |   246.0 ns |  6.01 ns |  13.94 ns |   240.1 ns |
|          ProtoNetSimpleMessage | 1,181.5 ns | 17.67 ns |  15.66 ns | 1,178.4 ns |
|               DasDoubleMessage |   298.3 ns |  3.41 ns |   2.85 ns |   297.5 ns |
|         ProtoNetDoubleMeessage | 1,276.8 ns | 46.75 ns | 132.63 ns | 1,219.6 ns |
|               DasStringMessage |   440.0 ns |  8.42 ns |   7.88 ns |   439.1 ns |
|          ProtoNetStringMessage | 1,538.8 ns | 30.60 ns |  81.15 ns | 1,500.4 ns |
|                  DasDictionary | 2,011.1 ns |  3.12 ns |   2.76 ns | 2,010.8 ns |
|       ProtoNetObjectDictionary | 2,818.8 ns | 15.41 ns |  14.42 ns | 2,822.4 ns |
|      DasNegativeIntegerMessage |   304.8 ns |  2.49 ns |   2.21 ns |   305.1 ns |
| ProtoNetNegativeIntegerMessage | 1,209.5 ns |  3.35 ns |   2.97 ns | 1,209.2 ns |
|             DasMultiProperties |   463.9 ns |  8.97 ns |   8.39 ns |   459.8 ns |
|        ProtoNetMultiProperties | 1,544.6 ns | 13.27 ns |  12.42 ns | 1,539.7 ns |
|             DasComposedMessage | 1,936.6 ns | 10.15 ns |   9.00 ns | 1,936.8 ns |
|        ProtoNetComposedMessage | 3,210.2 ns | 27.16 ns |  22.68 ns | 3,200.1 ns |
|                   DasByteArray |   290.6 ns |  2.95 ns |   2.76 ns |   290.1 ns |
|              ProtoNetByteArray | 1,251.8 ns | 11.08 ns |  10.36 ns | 1,248.3 ns |
