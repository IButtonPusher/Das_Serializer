``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                  Method |       Mean |    Error |   StdDev |
|------------------------ |-----------:|---------:|---------:|
|        DasSimpleMessage |   673.5 ns |  9.04 ns |  8.01 ns |
|   ProtoNetSimpleMessage | 1,159.5 ns |  7.24 ns |  6.04 ns |
|       DasDoubleMeessage |   892.8 ns |  4.59 ns |  4.07 ns |
|  ProtoNetDoubleMeessage | 1,214.1 ns | 31.13 ns | 27.59 ns |
|        DasStringMessage |   893.6 ns | 10.32 ns |  8.62 ns |
|   ProtoNetStringMessage | 1,503.2 ns | 17.13 ns | 15.18 ns |
|      DasMultiProperties | 1,168.6 ns | 14.94 ns | 13.97 ns |
| ProtoNetMultiProperties | 1,555.0 ns | 11.55 ns |  9.64 ns |
|      DasComposedMessage | 2,125.0 ns | 22.21 ns | 19.68 ns |
| ProtoNetComposedMessage | 1,790.2 ns |  6.45 ns |  5.38 ns |
|       ProtoNetByteArray | 1,260.6 ns | 11.38 ns |  9.50 ns |
|            DasByteArray |   758.3 ns |  3.20 ns |  3.00 ns |
