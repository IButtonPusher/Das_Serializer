``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                  Method |     Mean |     Error |    StdDev |
|------------------------ |---------:|----------:|----------:|
|        DasSimpleMessage | 1.008 us | 0.0065 us | 0.0058 us |
|   ProtoNetSimpleMessage | 1.165 us | 0.0074 us | 0.0069 us |
|       DasDoubleMeessage | 1.676 us | 0.0112 us | 0.0100 us |
|  ProtoNetDoubleMeessage | 1.202 us | 0.0065 us | 0.0060 us |
|        DasStringMessage | 1.364 us | 0.0096 us | 0.0089 us |
|   ProtoNetStringMessage | 1.495 us | 0.0102 us | 0.0091 us |
|      DasMultiProperties | 1.818 us | 0.0136 us | 0.0128 us |
| ProtoNetMultiProperties | 1.538 us | 0.0104 us | 0.0097 us |
|      DasComposedMessage | 3.463 us | 0.0079 us | 0.0074 us |
| ProtoNetComposedMessage | 1.837 us | 0.0099 us | 0.0092 us |
|       ProtoNetByteArray | 1.255 us | 0.0066 us | 0.0062 us |
|            DasByteArray | 3.044 us | 0.0124 us | 0.0116 us |
