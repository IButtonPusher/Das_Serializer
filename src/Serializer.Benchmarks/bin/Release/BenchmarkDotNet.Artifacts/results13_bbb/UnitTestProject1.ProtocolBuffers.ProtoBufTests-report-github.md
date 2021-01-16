``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                  Method |       Mean |    Error |   StdDev |
|------------------------ |-----------:|---------:|---------:|
|        DasSimpleMessage |   994.2 ns |  9.41 ns |  8.34 ns |
|   ProtoNetSimpleMessage | 1,174.4 ns |  8.73 ns |  7.74 ns |
|       DasDoubleMeessage | 1,163.4 ns |  5.98 ns |  4.67 ns |
|  ProtoNetDoubleMeessage | 1,189.4 ns |  4.34 ns |  4.06 ns |
|        DasStringMessage | 1,356.8 ns |  8.67 ns |  7.68 ns |
|   ProtoNetStringMessage | 1,492.4 ns | 12.34 ns | 11.54 ns |
|      DasMultiProperties | 1,811.3 ns |  9.25 ns |  8.20 ns |
| ProtoNetMultiProperties | 1,533.4 ns |  6.50 ns |  6.08 ns |
|      DasComposedMessage | 3,474.4 ns | 21.41 ns | 20.03 ns |
| ProtoNetComposedMessage | 1,813.8 ns | 14.16 ns | 13.24 ns |
|       ProtoNetByteArray | 1,265.0 ns |  9.00 ns |  8.42 ns |
|            DasByteArray | 3,061.3 ns | 25.25 ns | 23.62 ns |
