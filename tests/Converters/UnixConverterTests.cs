using System;
using Xunit;

namespace Maverick.Json.Converters
{
    public class UnixConverterTests
    {
        [Fact]
        public void Convert()
        {
            var time = new DateTime( 2000, 1, 1, 0, 0, 0, DateTimeKind.Utc );
            var json = JsonConvert.Serialize( new Test { Value = time } );

            Assert.Equal( "{\"Value\":946684800}", json );

            var deserializedTime = JsonConvert.Deserialize<Test>( json ).Value;

            Assert.Equal( time, deserializedTime );
        }


        private class Test
        {
            [JsonConverter( typeof( UnixDateTimeConverter ) )]
            public DateTime Value { get; set; }
        }
    }
}
