using System;
using System.Collections.Generic;
using Xunit;

namespace Maverick.Json.Issues
{
    public class ReadOnlyRequiredProperty
    {
        [Fact]
        public void ShouldNotThrowWhenExist()
        {
            var obj = JsonConvert.Deserialize<Object>( "{ \"Numbers\": [1,2,3] }" );

            Assert.NotNull( obj );
            Assert.Equal( new Int32[] { 1, 2, 3 }, obj.Numbers );
        }


        [Fact]
        public void ShouldThrowWhenMissing()
        {
            var ex = Assert.Throws<JsonSerializationException>( () => JsonConvert.Deserialize<Object>( "{}" ) );

            Assert.Equal( "Missing value for required property Numbers in Maverick.Json.Issues.ReadOnlyRequiredProperty+Object.", ex.Message );
        }


        private class Object
        {
            [JsonProperty( Required = true )]
            public List<Int32> Numbers { get; } = new List<Int32>();
        }
    }
}
