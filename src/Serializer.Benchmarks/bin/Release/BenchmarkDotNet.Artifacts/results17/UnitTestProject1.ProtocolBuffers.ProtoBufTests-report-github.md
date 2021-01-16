``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                  Method |       Mean |    Error |   StdDev |
|------------------------ |-----------:|---------:|---------:|
|        DasSimpleMessage |   669.0 ns |  7.81 ns |  7.31 ns |
|   ProtoNetSimpleMessage | 1,149.5 ns |  5.70 ns |  5.33 ns |
|       DasDoubleMeessage |   856.2 ns |  6.78 ns |  6.01 ns |
|  ProtoNetDoubleMeessage | 1,162.9 ns | 11.88 ns | 11.11 ns |
|        DasStringMessage |   895.4 ns |  4.12 ns |  3.86 ns |
|   ProtoNetStringMessage | 1,514.0 ns | 25.95 ns | 23.00 ns |
|      DasMultiProperties | 1,143.6 ns | 15.04 ns | 13.33 ns |
| ProtoNetMultiProperties | 1,589.6 ns | 31.60 ns | 43.25 ns |
|      DasComposedMessage | 2,066.6 ns |  9.40 ns |  8.79 ns |
| ProtoNetComposedMessage | 1,848.6 ns |  8.62 ns |  8.06 ns |
|       ProtoNetByteArray | 1,240.1 ns |  5.76 ns |  5.10 ns |
|            DasByteArray |   743.0 ns |  3.55 ns |  3.32 ns |
