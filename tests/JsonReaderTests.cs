using System;
using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Maverick.Json.TestObjects;
using Xunit;

namespace Maverick.Json
{
    public class JsonReaderTests
    {
        [Fact]
        public void CustomContructor()
        {
            var obj = JsonConvert.Deserialize<TestStructWithCustomCtor>( "{\"P\": \"12345\"}" );

            Assert.Equal( "12345", obj.Property );
        }


        [Theory]
        [InlineData( JsonNamingStrategy.Unspecified )]
        [InlineData( JsonNamingStrategy.CamelCase )]
        [InlineData( JsonNamingStrategy.SnakeCase )]
        public void Casing( JsonNamingStrategy strategy )
        {
            var settings = new JsonSettings
            {
                NamingStrategy = strategy
            };

            var obj = new
            {
                Name = "John",
                Age = 33,
                BirthDate = new DateTime( 2000, 01, 01 ),
                Dictionary = new Dictionary<String, String>
                {
                    [ "SomeProperty" ] = "Test",
                    [ "AnotherOneBytesTheDust" ] = "Another One Bytes the Dust"
                }
            };
            var json = JsonConvert.Serialize( obj, settings: settings );
            var deserialized = JsonConvert.DeserializeAnonymous( json, obj, settings );

            Assert.Equal( obj.Name, deserialized.Name );
            Assert.Equal( obj.Age, deserialized.Age );
            Assert.Equal( obj.BirthDate, deserialized.BirthDate );
            Assert.Equal( obj.Dictionary, deserialized.Dictionary );
        }


        [Fact]
        public void MissingRequriedProperty()
        {
            var obj = JsonConvert.Deserialize<TestObject>( "{ \"Name\": \"Tom\" }" );

            Assert.Equal( "Tom", obj.Name );
            Assert.Throws<JsonSerializationException>( () => JsonConvert.Deserialize<TestObject>( "{}" ) );
        }


        [Fact]
        public void PopulateReadOnlyCollectionProperty()
        {
            var obj = new TestReadOnlyCollectionsObject
            {
                Items = { "1", "2", "3" },
                Index = { "1", "2", "3" },
                Dictionary =
                {
                    [ 1 ] = new Decimal[] { 1.1M, 1.2M },
                    [ 2 ] = new Decimal[] { 2.1M, 2M }
                },
                EnumDictionary =
                {
                    [ JsonToken.Boolean ] = 1
                }
            };
            obj.Test.Name = "Someone";
            obj.Array[ 0 ] = "0";
            obj.Array[ 2 ] = "2";

            var json = JsonConvert.Serialize( obj );
            var deserialized = JsonConvert.Deserialize<TestReadOnlyCollectionsObject>( json );
            var json2 = JsonConvert.Serialize( deserialized );

            Assert.Equal( json, json2 );
        }


        [Fact]
        public void PopulateCustomCollection()
        {
            var index = new { List = new Index<Int32, Int32>( x => x ) };

            JsonConvert.Populate( "{ \"List\": [1, 2, 3] }", index );
            Assert.Equal( index.List, new[] { 1, 2, 3 } );
        }


        [Fact]
        public void PopulateHashSet()
        {
            var set = new { List = new HashSet<Int32>() };

            JsonConvert.Populate( "{ \"List\": [1, 2, 3] }", set );
            Assert.Equal( set.List, new[] { 1, 2, 3 } );
        }


        [Fact]
        public void ReadStructWithFields()
        {
            var x = JsonConvert.Deserialize<TestStructWithFields>( "{ \"Name\": \"Ivan\", \"Age\": 31 }" );

            Assert.Equal( "Ivan", x.Name );
            Assert.Equal( 31, x.Age );
        }


        [Fact]
        public void ReadValueTuple()
        {
            var x = JsonConvert.Deserialize<(string Name, int Age)>( "{ \"Item1\": \"Ivan\", \"Item2\": 31 }" );

            Assert.Equal( "Ivan", x.Name );
            Assert.Equal( 31, x.Age );
        }


        [Fact]
        public void JsonWithMissingCommasShouldFail()
        {
            var ex = Assert.Throws<JsonSerializationException>( () => JsonConvert.Deserialize<Dictionary<String, Object>>( "{ \"1\": 1 \"2\": 2 }" ) );
            Assert.Equal( "Detected invalid JSON. PropertyName is missing comma before it.", ex.Message );

            ex = Assert.Throws<JsonSerializationException>( () => JsonConvert.Deserialize<Object[]>( "[1 2 3]" ) );
            Assert.Equal( "Detected invalid JSON. Number is missing comma before it.", ex.Message );
        }


        [Fact]
        public void ReadingInterfaceFromNull()
        {
            var obj = JsonConvert.Deserialize<IConvertible>( "null" );

            Assert.Null( obj );
        }


        [Fact]
        public void ReadingArrayFromNull()
        {
            var obj = JsonConvert.Deserialize<Int32[]>( "null" );

            Assert.Null( obj );
        }


        [Fact]
        public void ReadLargeByteArray()
        {
            var data = new Byte[ 100000 ];

            RandomNumberGenerator.Fill( data );

            var base64 = "\"" + Convert.ToBase64String( data ) + "\"";
            var deserializedData = JsonConvert.Deserialize<Byte[]>( base64 );

            Assert.Equal( data, deserializedData );
        }


        [Fact]
        public void ReadValueWithLotsOfWhiteSpace()
        {
            var obj = JsonConvert.Deserialize<TestObject>( "{   \"Name\"  :   \"Test\"   }" );

            Assert.Equal( "Test", obj.Name );
        }


        [Fact]
        public void Skip()
        {
            var json = JsonConvert.Serialize( new
            {
                Name = "Not a Random String",
                Age = 22,
                Enabled = true,
                Null = default( Int32? ),
                Array = new Object[] { "String", 0.0, true },
                Addresses = new
                {
                    Country = new
                    {
                        Name = "France"
                    }
                }
            }, settings: new JsonSettings
            {
                Format = JsonFormat.Indented,
                SerializeNulls = true
            } );
            var reader = CreateReader( json );

            // The skip should ignore the entire json
            reader.Skip();

            Assert.Equal( JsonToken.None, reader.Peek() );
        }


        [Fact]
        public void SetStateRestoreThenSkipInArray()
        {
            var reader = CreateReader( JsonConvert.Serialize( new
            {
                Name = "123",
                Items = new[]
                {
                    new
                    {
                        Type = "321",
                        Number = 3
                    }
                }
            } ) );

            reader.ReadStartObject();

            Assert.Equal( "Name", reader.ReadPropertyName() );
            Assert.Equal( "123", reader.ReadString() );
            Assert.Equal( "Items", reader.ReadPropertyName() );

            reader.ReadStartArray();

            var state = reader.GetState();
            reader.ReadStartObject();

            Assert.Equal( "Type", reader.ReadPropertyName() );
            Assert.Equal( "321", reader.ReadString() );

            reader.SetState( state );
            reader.Skip();

            reader.ReadEndArray();
            reader.ReadEndObject();
        }


        [Fact]
        public void ReaderShouldAdvanceSequenceReader()
        {
            var reader = CreateReader( "\"A String\"" );

            Assert.Equal( "A String", reader.ReadString() );
            Assert.Equal( 0, reader.Sequence.Slice( reader.Position ).Length );
        }


        private static JsonReader CreateReader( String json )
        {
            var bytes = Encoding.UTF8.GetBytes( json );

            return new JsonReader( new ReadOnlySequence<Byte>( bytes ) );
        }


        private struct TestStructWithCustomCtor
        {
            private TestStructWithCustomCtor( Int32 property )
            {
                Property = property.ToString();
            }


            private TestStructWithCustomCtor( String property )
            {
                Property = property;
            }


            [JsonProperty( "P" )]
            public String Property { get; }
        }


        private sealed class TestObject
        {
            [JsonProperty( Required = true )]
            public String Name { get; set; }
        }


        private sealed class TestReadOnlyCollectionsObject
        {
            public IList<String> Items { get; } = new List<String>();
            public Index<String, String> Index { get; } = new Index<String, String>( x => x[ 0 ].ToString() );
            public String[] Array { get; } = new String[ 3 ];
            [JsonConverter( typeof( Converter ) )]
            public Dictionary<Byte, Decimal[]> Dictionary { get; } = new Dictionary<Byte, Decimal[]>();
            public Dictionary<JsonToken, Int32> EnumDictionary { get; } = new Dictionary<JsonToken, Int32>();
            public TestObject Test { get; } = new TestObject();


            private sealed class Converter : JsonConverter<Dictionary<Byte, Decimal[]>>
            {
                public override Dictionary<Byte, Decimal[]> Read( JsonReader reader, Type objectType )
                {
                    var dictionary = new Dictionary<Byte, Decimal[]>();

                    reader.ReadStartObject();

                    while ( reader.Peek() == JsonToken.PropertyName )
                    {
                        dictionary.Add( Byte.Parse( reader.ReadPropertyName() ), reader.ReadValue<Decimal[]>() );
                    }

                    reader.ReadEndObject();

                    return dictionary;
                }

                public override void Write( JsonWriter writer, Dictionary<Byte, Decimal[]> value )
                {
                    writer.WriteStartObject();

                    foreach ( var item in value )
                    {
                        writer.WritePropertyName( item.Key.ToString() );
                        writer.WriteValue( item.Value );
                    }

                    writer.WriteEndObject();
                }
            }
        }


        private struct TestStructWithFields
        {
            public TestStructWithFields( IEnumerable<Int32> unused )
            {
                Name = null;
                Age = 0;
            }


            public String Name;
            public Int32 Age;
        }
    }
}
