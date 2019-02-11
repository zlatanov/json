A fully featured, non allocating UTF-8 Json serializer.
========================================
[![Build Status](https://dev.azure.com/zlatanov/GitHub%20Open%20Source/_apis/build/status/zlatanov.json?branchName=master)](https://dev.azure.com/zlatanov/GitHub%20Open%20Source/_build/latest?definitionId=1&branchName=master)

#### Example
```c#
var settings = new JsonSettings
{
    NamingStrategy = JsonNamingStrategy.SnakeCase,
    Format = JsonFormat.Indented
};

JsonConvert.Serialize( new
{
    FirstName = "Jack",
    LastName = "Reacher",
    Age = 33
}, settings: settings );

// {
//     "first_name": "Jack",
//     "last_name": "Reacher",
//     "age": 33
// }

```

Supported features:

1. Almost zero allocations
2. Custom constructors
3. Custom converters
4. Property naming strategy that works with custom converters as well
5. Built in support for collections and read only reference properties
6. [JsonIgnore]
7. Formatting - None, White Spaced, Indented
8. Ignoring nulls when serializing
9. Custom property names and order.
10. Required properties.

### Benchmarks
BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.523 (1803/April2018Update/Redstone4)
Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3320316 Hz, Resolution=301.1762 ns, Timer=TSC
.NET Core SDK=2.2.101
  [Host]    : .NET Core 2.2.0 (CoreCLR 4.6.27110.04, CoreFX 4.6.27110.04), 64bit RyuJIT
  RyuJitX64 : .NET Core 2.2.0 (CoreCLR 4.6.27110.04, CoreFX 4.6.27110.04), 64bit RyuJIT

Job=RyuJitX64  Jit=RyuJit  Platform=X64  
Categories=Write  

Writing 100,000 items to Stream.Null. Newtosoft.Json has been setup to use array pooling.
```
|          Method |     Mean |    Error |   StdDev | Ratio | RatioSD | Gen 0/1k Op | Allocated Memory/Op |
|---------------- |---------:|---------:|---------:|------:|--------:|------------:|--------------------:|
| WriteNewtonsoft | 530.5 ms | 6.346 ms | 5.626 ms |  1.00 |    0.00 |  18000.0000 |         77221.88 KB |
|   WriteMaverick | 416.8 ms | 6.007 ms | 5.619 ms |  0.79 |    0.02 |           - |            37.17 KB |
