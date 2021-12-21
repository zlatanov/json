using System;
using System.Text;
using Xunit;

namespace Maverick.Json.Issues
{
    public class ReadNullForInterfaceProperty
    {
        record Test( IFormattable Name );

        [Fact]
        public void NullShouldBeCorrectlyHandledWhenTheTargetTypeIsInterface()
        {
            Assert.Null( JsonConvert.Deserialize<IFormattable>( "null" ) );
            Assert.Null( JsonConvert.Deserialize<Test>( "{ \"Name\": null }" ).Name );

            var reader = new JsonReader( new( Encoding.UTF8.GetBytes( "null" ) ) );
            Assert.Null( reader.ReadValue<IFormattable>() );

            reader = new JsonReader( new( Encoding.UTF8.GetBytes( "null" ) ) );
            Assert.Null( reader.ReadValue( typeof( IFormattable ) ) );
        }
    }
}
