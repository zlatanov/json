using System;
using Xunit;

namespace Maverick.Json
{
    public sealed class JsonIgnoreTests
    {
        [Fact]
        public void IgnoredPropertyShouldNotBeSerialized()
        {
            var json = JsonConvert.Serialize( new TestObject
            {
                Name = "John Doe",
                Age = 33
            } );

            Assert.DoesNotContain( "John Doe", json );
        }


        [Fact]
        public void IgnoredPropertyShouldNotBeDeserialized()
        {
            var obj = JsonConvert.Deserialize<TestObject>( "{\"Name\": \"John Doe\", \"Age\": 33}" );

            Assert.Equal( 33, obj.Age );
            Assert.Null( obj.Name );
        }


        [Fact]
        public void IgnoredPropertyShouldNotBePopulated()
        {
            var obj = new TestObject();

            JsonConvert.Populate( "{\"Name\": \"John Doe\", \"Age\": 33}", obj );

            Assert.Equal( 33, obj.Age );
            Assert.Null( obj.Name );
        }


        private sealed class TestObject
        {
            [JsonIgnore]
            public String Name { get; set; }


            public Int32 Age { get; set; }
        }
    }
}
