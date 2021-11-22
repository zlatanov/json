using System;
using System.Reflection;
using Maverick.Json.Serialization;
using Xunit;

namespace Maverick.Json.Issues
{
    public class WriterShouldNotIgnorePropertyConverter
    {
        [Fact]
        public void ShouldUsePropertyConverter()
        {
            var json = JsonConvert.Serialize( new Test { Age = new() { Value = 17 } }, settings: new JsonSettings
            {
                ContractResolver = new Resolver()
            } );

            Assert.Equal( "{\"Age\":17}", json );
        }

        private sealed class Test
        {
            public Age Age { get; init; }
        }

        [JsonConverter( typeof( ConverterThatThrows ) )]
        private struct Age
        {
            public int Value { get; set; }
        }

        private sealed class Resolver : JsonContractResolver
        {
            protected override JsonProperty<TOwner, TProperty> CreateProperty<TOwner, TProperty>( JsonObjectContract<TOwner> contract, MemberInfo member )
            {
                var property = base.CreateProperty<TOwner, TProperty>( contract, member );

                if ( property.PropertyType == typeof( Age ) )
                {
                    property.Converter = new Converter();
                }

                return property;
            }
        }

        private sealed class Converter : JsonConverter<Age>
        {
            public override Age Read( JsonReader reader, Type objectType )
            {
                return new Age
                {
                    Value = reader.ReadInt32()
                };
            }

            public override void Write( JsonWriter writer, Age value )
            {
                writer.WriteValue( value.Value );
            }
        }

        private sealed class ConverterThatThrows : JsonConverter<Age>
        {
            public override Age Read( JsonReader reader, Type objectType )
            {
                throw new NotSupportedException();
            }

            public override void Write( JsonWriter writer, Age value )
            {
                throw new NotSupportedException();
            }
        }
    }
}
