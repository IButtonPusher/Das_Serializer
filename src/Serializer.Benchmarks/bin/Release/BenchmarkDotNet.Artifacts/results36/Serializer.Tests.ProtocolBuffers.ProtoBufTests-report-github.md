``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
AMD Ryzen Threadripper 2950X, 1 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4075.0), X64 RyuJIT


```
|                         Method |       Mean |    Error |   StdDev |
|------------------------------- |-----------:|---------:|---------:|
|               DasSimpleMessage |   249.0 ns |  4.97 ns |  6.28 ns |
|          ProtoNetSimpleMessage | 1,177.0 ns | 12.77 ns | 11.32 ns |
|               DasDoubleMessage |   278.1 ns |  5.48 ns |  4.86 ns |
|         ProtoNetDoubleMeessage | 1,186.4 ns |  6.94 ns |  6.49 ns |
|               DasStringMessage |   426.3 ns |  3.34 ns |  3.12 ns |
|          ProtoNetStringMessage | 1,488.0 ns | 20.66 ns | 19.33 ns |
|                  DasDictionary | 5,632.2 ns | 18.11 ns | 16.94 ns |
|       ProtoNetObjectDictionary | 2,853.2 ns | 15.44 ns | 13.69 ns |
|      DasNegativeIntegerMessage |   297.5 ns |  5.70 ns |  5.60 ns |
| ProtoNetNegativeIntegerMessage | 1,201.8 ns |  5.52 ns |  5.16 ns |
|             DasMultiProperties |   466.7 ns |  3.14 ns |  2.94 ns |
|        ProtoNetMultiProperties | 1,592.3 ns | 33.11 ns | 47.49 ns |
|             DasComposedMessage | 1,961.5 ns | 22.87 ns | 21.40 ns |
|        ProtoNetComposedMessage | 3,258.7 ns | 23.45 ns | 20.78 ns |
|                   DasByteArray |   293.4 ns |  2.70 ns |  2.53 ns |
|              ProtoNetByteArray | 1,262.4 ns | 17.50 ns | 16.37 ns |
