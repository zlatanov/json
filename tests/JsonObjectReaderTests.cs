using System;
using System.Buffers;
using System.Text;
using Xunit;

namespace Maverick.Json
{
    public class JsonObjectReaderTests
    {
        [Fact]
        public void ReadMultipleTimes()
        {
            var json = Encoding.UTF8.GetBytes( JsonConvert.Serialize( new
            {
                Name = "Agent Smith",
                Age = 44,
                Location = "South Pacific"
            } ) );
            var reader = new JsonObjectReader( new ReadOnlySequence<Byte>( json ) );

            Assert.Equal( new[] { "Name", "Age", "Location" }, reader.PropertyNames );

            for ( var i = 0; i < 2; ++i )
            {
                Assert.Equal( "South Pacific", reader.Read<String>( "Location" ) );
                Assert.Equal( 44, reader.Read<Int32>( "Age" ) );
                Assert.Equal( "Agent Smith", reader.Read<String>( "Name" ) );
            }

            // We aren't doing any caching
            var str1 = reader.Read<String>( "Name" );
            var str2 = reader.Read<String>( "Name" );

            Assert.Equal( str1, str2 );
            Assert.False( ReferenceEquals( str1, str2 ) );
        }
    }
}
