# json
A fully featured UTF-8 Json serializer.

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
