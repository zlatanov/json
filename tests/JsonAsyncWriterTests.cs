using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maverick.Json.Async;
using Xunit;

namespace Maverick.Json
{
    public class JsonAsyncWriterTests
    {
        [Fact]
        public async Task Success()
        {
            var json = await JsonConvert.SerializeAsync( 10 );

            Assert.Equal( "10", json );
        }
    }
}
