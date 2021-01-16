``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                  Method |       Mean |    Error |   StdDev |
|------------------------ |-----------:|---------:|---------:|
|        DasSimpleMessage |   723.4 ns |  7.50 ns |  6.26 ns |
|   ProtoNetSimpleMessage | 1,182.5 ns | 23.13 ns | 26.64 ns |
|       DasDoubleMeessage |   929.0 ns |  5.74 ns |  5.08 ns |
|  ProtoNetDoubleMeessage | 1,205.4 ns | 13.10 ns | 11.62 ns |
|        DasStringMessage |   919.0 ns |  5.49 ns |  4.59 ns |
|   ProtoNetStringMessage | 1,470.6 ns |  6.34 ns |  5.62 ns |
|      DasMultiProperties | 1,219.5 ns | 23.53 ns | 27.09 ns |
| ProtoNetMultiProperties | 1,572.1 ns | 24.42 ns | 21.64 ns |
|      DasComposedMessage | 2,278.3 ns | 30.38 ns | 28.42 ns |
| ProtoNetComposedMessage | 1,830.6 ns | 13.78 ns | 12.89 ns |
|       ProtoNetByteArray | 1,264.9 ns |  9.34 ns |  8.28 ns |
|            DasByteArray |   804.8 ns |  5.98 ns |  4.99 ns |
