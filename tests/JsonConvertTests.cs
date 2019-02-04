using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace Maverick.Json
{
    public class JsonConvertTests
    {
        [Fact]
        public void SerializeToStream()
        {
            var stream = new MemoryStream();

            JsonConvert.Serialize( stream, new
            {
                Name = "Jack Reacher"
            } );

            var reader = new JsonReader( new ReadOnlySequence<Byte>( stream.ToArray() ) );

            reader.ReadStartObject();

            Assert.Equal( "Name", reader.ReadPropertyName() );
            Assert.Equal( "Jack Reacher", reader.ReadString() );

            reader.ReadEndObject();
        }
    }
}
