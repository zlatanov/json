using System;
using Maverick.Json.Converters;
using Xunit;

namespace Maverick.Json
{
    public class JsonConverterTests
    {
        [Fact]
        public void OpenGenericConverter()
        {
            var json = "[123,345]";
            var obj = JsonConvert.Deserialize<TestObject<Int32, Int32>>( json );

            Assert.Equal( 123, obj.P1 );
            Assert.Equal( 345, obj.P2 );
            Assert.Equal( json, JsonConvert.Serialize( obj ) );
        }


        [Fact]
        public void PopulateShouldNotUseConverter()
        {
            var json = "{\"P1\": 1, \"P2\": 2.5}";
            var obj = new TestObject<Int32, Decimal>();

            JsonConvert.Populate( json, obj );

            Assert.Equal( 1, obj.P1 );
            Assert.Equal( 2.5M, obj.P2 );
        }


        [Fact]
        public void UsingPropertyConverter()
        {
            var obj = new TestObject
            {
                Bytes = new Byte[] { 1, 2, 3 },
                Boolean = true
            };
            var json = JsonConvert.Serialize( obj );

            Assert.Equal( "{\"Bytes\":[1,2,3],\"Boolean\":1}", json );

            var obj2 = JsonConvert.Deserialize<TestObject>( json );

            Assert.Equal( obj.Bytes, obj2.Bytes );
            Assert.Equal( obj.Boolean, obj2.Boolean );
        }


        [Fact]
        public void UsingInterfaceConverter()
        {
            var obj = new InterfaceObject { Age = 33 };
            var json = JsonConvert.Serialize( obj );
            var deserializedObj = JsonConvert.Deserialize<InterfaceObject>( json );

            Assert.DoesNotContain( "UsedConverter", json );
            Assert.Equal( obj.Age, deserializedObj.Age );
            Assert.True( obj.UsedConverter, "Object serialize converter not called." );
            Assert.True( deserializedObj.UsedConverter, "Object deserialize converter not called." );
        }
        

        [JsonConverter( typeof( TestObject<,>.Converter ) )]
        private sealed class TestObject<T1, T2>
        {
            public T1 P1 { get; set; }
            public T2 P2 { get; set; }

            private sealed class Converter : JsonConverter<TestObject<T1, T2>>
            {
                public override TestObject<T1, T2> Read( JsonReader reader, Type objectType )
                {
                    var result = new TestObject<T1, T2>();

                    reader.ReadStartArray();
                    result.P1 = reader.ReadValue<T1>();
                    result.P2 = reader.ReadValue<T2>();
                    reader.ReadEndArray();

                    return result;
                }

                public override void Write( JsonWriter writer, TestObject<T1, T2> value )
                {
                    writer.WriteStartArray();
                    writer.WriteValue( value.P1 );
                    writer.WriteValue( value.P2 );
                    writer.WriteEndArray();
                }
            }
        }


        private sealed class TestObject
        {
            [ByteArrayConverter]
            public Byte[] Bytes { get; set; }


            [JsonConverter( typeof( BooleanConverter ) )]
            public Boolean Boolean { get; set; }


            private sealed class BooleanConverter : JsonConverter<Boolean>
            {
                public override Boolean Read( JsonReader reader, Type objectType )
                {
                    return reader.ReadByte() == 1;
                }


                public override void Write( JsonWriter writer, Boolean value )
                {
                    writer.WriteValue( value ? 1 : 0 );
                }
            }
        }


        [JsonConverter( typeof( InterfaceConverter ) )]
        private interface IInterface
        {
            Int32 Age { get; }


            [JsonIgnore]
            Boolean UsedConverter { get; set; }
        }


        private sealed class InterfaceConverter : JsonConverter<IInterface>
        {
            public override IInterface Read( JsonReader reader, Type objectType )
            {
                var value = (IInterface)reader.ReadValueIgnoreConverter( objectType );
                value.UsedConverter = true;

                return value;
            }

            public override void Write( JsonWriter writer, IInterface value )
            {
                writer.WriteValueIgnoreConverter( value );
                value.UsedConverter = true;
            }
        }


        private sealed class InterfaceObject : IInterface
        {
            public Int32 Age { get; set; }


            public Boolean UsedConverter { get; set; }
        }
    }
}
