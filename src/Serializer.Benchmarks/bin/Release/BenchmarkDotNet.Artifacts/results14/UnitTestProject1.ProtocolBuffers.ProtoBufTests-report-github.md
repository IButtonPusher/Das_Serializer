``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                  Method |       Mean |    Error |   StdDev |
|------------------------ |-----------:|---------:|---------:|
|        DasSimpleMessage |   671.3 ns |  5.39 ns |  4.77 ns |
|   ProtoNetSimpleMessage | 1,184.2 ns | 21.73 ns | 20.33 ns |
|       DasDoubleMeessage |   880.5 ns |  8.52 ns |  7.97 ns |
|  ProtoNetDoubleMeessage | 1,243.9 ns | 18.51 ns | 17.31 ns |
|        DasStringMessage |   893.2 ns | 16.23 ns | 15.18 ns |
|   ProtoNetStringMessage | 1,528.2 ns | 26.20 ns | 23.22 ns |
|      DasMultiProperties | 1,129.9 ns | 10.71 ns | 10.02 ns |
| ProtoNetMultiProperties | 1,585.6 ns | 13.30 ns | 12.44 ns |
|      DasComposedMessage | 2,212.8 ns | 36.50 ns | 34.14 ns |
| ProtoNetComposedMessage | 1,808.7 ns | 12.64 ns | 11.82 ns |
|       ProtoNetByteArray | 1,299.0 ns | 13.81 ns | 12.92 ns |
|            DasByteArray |   743.7 ns |  8.56 ns |  7.58 ns |
