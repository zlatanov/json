using Xunit;

namespace Maverick.Json
{
    public class JsonReaderStackTests
    {
        [Fact]
        public void BasicTest()
        {
            var stack = new JsonReaderStack();

            stack.PushStartObject();
            stack.PushStartArray();
            stack.PushStartObject();
            stack.PushStartArray();

            Assert.Equal( 4, stack.Depth );
            Assert.Equal( JsonToken.StartArray, stack.Current );

            stack.Pop();
            Assert.Equal( JsonToken.StartObject, stack.Current );

            stack.Pop();
            Assert.Equal( JsonToken.StartArray, stack.Current );

            stack.Pop();
            Assert.Equal( JsonToken.StartObject, stack.Current );

            stack.Pop();
            Assert.Equal( JsonToken.None, stack.Current );
            Assert.Equal( 0, stack.Depth );
        }


        [Fact]
        public void MaxDepth()
        {
            var stack = new JsonReaderStack();

            Assert.Equal( 0, stack.Depth );

            for ( var i = 0; i < JsonReaderStack.MaxDepth; ++i )
            {
                stack.PushStartObject();

                Assert.Equal( i + 1, stack.Depth );
            }

            Assert.Equal( JsonReaderStack.MaxDepth, stack.Depth );
            Assert.Throws<JsonSerializationException>( stack.PushStartObject );
        }
    }
}
