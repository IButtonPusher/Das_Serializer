``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |     Error |    StdDev |
|------------------------------- |-----------:|----------:|----------:|
|               DasSimpleMessage |   579.0 ns |   8.22 ns |   7.29 ns |
|          ProtoNetSimpleMessage | 1,199.6 ns |   9.75 ns |   9.12 ns |
|               DasDoubleMessage |   664.4 ns |   2.87 ns |   2.55 ns |
|         ProtoNetDoubleMeessage | 1,185.1 ns |   6.89 ns |   6.44 ns |
|               DasStringMessage |   766.3 ns |   6.51 ns |   6.09 ns |
|          ProtoNetStringMessage | 1,465.1 ns |   5.40 ns |   4.79 ns |
|                  DasDictionary | 5,583.7 ns |  30.63 ns |  28.65 ns |
|       ProtoNetObjectDictionary | 2,942.5 ns |  60.52 ns |  62.15 ns |
|      DasNegativeIntegerMessage |   642.0 ns |   4.97 ns |   4.41 ns |
| ProtoNetNegativeIntegerMessage | 1,262.5 ns |  28.36 ns |  34.83 ns |
|             DasMultiProperties |   962.4 ns |   7.22 ns |   6.75 ns |
|         DasMultiProperties_OLD | 1,164.4 ns |  21.86 ns |  23.39 ns |
|        ProtoNetMultiProperties | 1,564.8 ns |  21.24 ns |  18.83 ns |
|             DasComposedMessage | 5,318.1 ns | 102.60 ns | 118.15 ns |
|        ProtoNetComposedMessage | 3,214.1 ns |  24.88 ns |  22.06 ns |
|                   DasByteArray |   656.4 ns |   5.73 ns |   5.08 ns |
|              ProtoNetByteArray | 1,255.2 ns |  11.12 ns |   9.86 ns |
