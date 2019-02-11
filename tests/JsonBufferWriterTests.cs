using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Maverick.Json
{
    public class JsonBufferWriterTests
    {
        [Fact]
        public void ToStringShouldReturnCorrectJson()
        {
            using ( var buffer = new JsonBufferWriter() )
            {
                var obj = new
                {
                    Name = "Tony Stark",
                    Occupation = "Avenger",
                    Planet = "Earth"
                };
                new JsonWriter( buffer ).WriteValue( obj );

                var json = buffer.ToString();

                Assert.Equal( Newtonsoft.Json.JsonConvert.SerializeObject( obj ), json );
            }
        }


        [Fact]
        public async Task UseAfterDisposeShouldThrow()
        {
            var buffer = new JsonBufferWriter();

            buffer.Write( "123456789" );
            Assert.Equal( "123456789", buffer.ToString() );

            buffer.Dispose();

            Assert.Throws<ObjectDisposedException>( () => buffer.GetSpan() );
            Assert.Throws<ObjectDisposedException>( () => buffer.GetMemory() );
            Assert.Throws<ObjectDisposedException>( () => buffer.ToArray() );
            Assert.Throws<ObjectDisposedException>( () => buffer.CopyTo( Stream.Null ) );
            Assert.Throws<ObjectDisposedException>( () => buffer.Write( "123" ) );
            await Assert.ThrowsAsync<ObjectDisposedException>( () => buffer.CopyToAsync( Stream.Null ) );

            // These should never throw
            Assert.Equal( String.Empty, buffer.ToString() );
            Assert.Equal( 0, buffer.Sequence.Length );
        }
    }
}
