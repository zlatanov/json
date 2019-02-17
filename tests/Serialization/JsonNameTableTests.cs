using System;
using System.Linq;
using Xunit;

namespace Maverick.Json.Serialization
{
    public class JsonNameTableTests
    {
        [Fact]
        public void Basic()
        {
            var table = new JsonNameTable();
            var count = 10000;

            foreach ( var propertyName in Enumerable.Range( 1, count )
                                                    .Select( x => "Property" + x ) )
            {
                table.Add( GetBytes( propertyName ), propertyName );
            }

            foreach ( var propertyName in Enumerable.Range( 1, count )
                                                    .Select( x => "Property" + x ) )
            {
                Assert.Equal( propertyName, table.Find( GetBytes( propertyName ) ) );
            }
        }


        private static ReadOnlyMemory<Byte> GetBytes( String propertyName )
            => Constants.Encoding.GetBytes( propertyName );
    }
}
