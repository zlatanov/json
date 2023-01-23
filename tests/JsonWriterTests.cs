using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Maverick.Json.TestObjects;
using Xunit;

namespace Maverick.Json
{
    public sealed class JsonWriterTests
    {
        [Fact]
        public void PropertyWithShouldSerialize()
        {
            var propertyName = $"\"{nameof( Dog.Owner )}\"";
            var json = JsonConvert.Serialize( new Dog() );
            Assert.DoesNotContain( propertyName, json );

            json = JsonConvert.Serialize( new Dog { Owner = "Me" } );
            Assert.Contains( propertyName, json );
        }


        [Fact]
        public void CircularReferenceShouldThrow()
        {
            var exception = Assert.Throws<JsonSerializationException>( () =>
                JsonConvert.Serialize( new CircularObject() ) );

            Assert.Contains( "Self referencing loop detected", exception.Message );
        }


        [Fact]
        public void PropertyWithNullValueShouldSerialize()
        {
            var json = JsonConvert.Serialize( new NullablePropertyObject() );

            Assert.Equal( "{\"Value\":null}", json );
        }


        [Fact]
        public void WhiteSpaceFormat()
        {
            var json = JsonConvert.Serialize( new NullablePropertyObject(), JsonFormat.WhiteSpace );
            Assert.Equal( "{ \"Value\": null }", json );

            json = JsonConvert.Serialize( new[] { 1, 2, 3 }, JsonFormat.WhiteSpace );
            Assert.Equal( "[ 1, 2, 3 ]", json );
        }


        [Fact]
        public void WriteLongString()
        {
            var value = new String( 'x', 1024 * 1024 );
            var json = JsonConvert.Serialize( value );
            var deserializedValue = JsonConvert.Deserialize<String>( json );

            Assert.Equal( value, deserializedValue );
        }


        [Theory]
        [InlineData( "1", "1" )]
        [InlineData( "1.00", "1" )]
        [InlineData( "1.23000", "1.23" )]
        [InlineData( "1000000", "1000000" )]
        public void WriteDecimal( String number, String expectedOutput )
        {
            var json = JsonConvert.Serialize( Decimal.Parse( number, CultureInfo.InvariantCulture ) );

            Assert.Equal( expectedOutput, json );
        }


        [Fact]
        public void WriteManyDoublesShouldNotCauseBufferIsTooSmall()
        {
            var array = Enumerable.Repeat( 0, 100000 ).Select( x => 0.317992417681495 );

            JsonConvert.Serialize( array );
        }


        [Fact]
        public void ByteArrayShouldNotInfiniteLoop()
        {
            var data = new Byte[ 10000 ];

            RandomNumberGenerator.Fill( data );

            var json = JsonConvert.Serialize( data );
            var base64 = Convert.ToBase64String( data );

            Assert.Equal( "\"" + base64 + "\"", json );
        }


        [Fact]
        public void DelegateShouldNotBeSerialized()
        {
            var json = JsonConvert.Serialize( new
            {
                Delegate = new Func<String>( () => "Should Not Be Serialized" )
            } );

            Assert.Equal( "{}", json );
        }


        [Fact]
        public void SpanShouldNotBeSerialized()
        {
            var memory = new Memory<Byte>( new Byte[ 1 ] { 1 } );
            var json = JsonConvert.Serialize( memory );

            Assert.Equal( "{\"Length\":1,\"IsEmpty\":false}", json );
        }


        [Fact]
        public void StringDictionaryShouldIgnoreNamingStrategy()
        {
            var json = JsonConvert.Serialize( new Dictionary<String, Int32>
            {
                [ "John Doe" ] = 33
            }, JsonFormat.None, settings: new JsonSettings { NamingStrategy = JsonNamingStrategy.SnakeCase } );

            Assert.Equal( "{\"John Doe\":33}", json );
        }


        [Fact]
        public void EnumDictionaryShouldAdhereToNamingStrategy()
        {
            var json = JsonConvert.Serialize( new Dictionary<JsonNamingStrategy, Int32>
            {
                [ JsonNamingStrategy.SnakeCase ] = 33
            }, settings: new JsonSettings { NamingStrategy = JsonNamingStrategy.SnakeCase } );

            Assert.Equal( "{\"snake_case\":33}", json );
        }


        [Fact]
        public void DynamicObjectsShouldAdhereToNamingStrategy()
        {
            dynamic obj = new ExpandoObject();
            obj.LastName = "X";

            var json = JsonConvert.Serialize( obj, settings: new JsonSettings { NamingStrategy = JsonNamingStrategy.SnakeCase } );
            Assert.Equal( "{\"last_name\":\"X\"}", json );
        }


        private sealed class CircularObject
        {
            public CircularObject() => Parent = this;
            public CircularObject Parent { get; set; }
        }


        private sealed class NullablePropertyObject
        {
            [JsonProperty( SerializeNulls = true )]
            public Int32? Value { get; set; }


            public Int32? AnotherValue { get; set; }
        }
    }
}
