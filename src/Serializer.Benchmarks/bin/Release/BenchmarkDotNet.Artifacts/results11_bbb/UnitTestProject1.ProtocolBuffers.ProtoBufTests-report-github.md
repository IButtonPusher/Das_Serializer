``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                  Method |     Mean |     Error |    StdDev |
|------------------------ |---------:|----------:|----------:|
|        DasSimpleMessage | 1.139 us | 0.0075 us | 0.0071 us |
|   ProtoNetSimpleMessage | 1.151 us | 0.0086 us | 0.0081 us |
|       DasDoubleMeessage | 1.850 us | 0.0082 us | 0.0072 us |
|  ProtoNetDoubleMeessage | 1.194 us | 0.0064 us | 0.0056 us |
|        DasStringMessage | 1.522 us | 0.0081 us | 0.0076 us |
|   ProtoNetStringMessage | 1.515 us | 0.0064 us | 0.0059 us |
|      DasMultiProperties | 2.063 us | 0.0204 us | 0.0191 us |
| ProtoNetMultiProperties | 1.548 us | 0.0161 us | 0.0150 us |
|      DasComposedMessage | 4.111 us | 0.0337 us | 0.0315 us |
| ProtoNetComposedMessage | 1.804 us | 0.0139 us | 0.0130 us |
|       ProtoNetByteArray | 1.245 us | 0.0107 us | 0.0095 us |
|            DasByteArray | 4.495 us | 0.0286 us | 0.0268 us |
