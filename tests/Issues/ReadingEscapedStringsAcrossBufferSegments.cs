using System;
using System.Buffers;
using Xunit;

namespace Maverick.Json.Issues
{
    public class ReadingEscapedStringsAcrossBufferSegments
    {
        private class MyArrayPool : ArrayPool<byte>
        {
            public override byte[] Rent( int minimumLength ) => new byte[ minimumLength ];

            public override void Return( byte[] array, bool clearArray = false )
            {
            }
        }

        [Fact]
        public void ShouldReadPropertyAndValueCorrectly()
        {
            using var buffer = new JsonBufferWriter( 8, new MyArrayPool() );
            buffer.Write( """
{"A": "\""}
""" );

            var reader = new JsonReader( buffer.Sequence );

            while ( true )
            {
                var type = reader.Peek();

                switch ( type )
                {
                    case JsonToken.PropertyName:
                        reader.ReadPropertyName();
                        break;

                    case JsonToken.StartObject:
                        reader.ReadStartObject();
                        break;

                    case JsonToken.StartArray:
                        reader.ReadStartArray();
                        break;

                    case JsonToken.EndObject:
                        reader.ReadEndObject();
                        break;

                    case JsonToken.EndArray:
                        reader.ReadEndArray();
                        break;

                    case JsonToken.Number:
                        reader.ReadDecimal();
                        break;

                    case JsonToken.String:
                        reader.ReadString();
                        break;

                    case JsonToken.Boolean:
                        reader.ReadBoolean();
                        break;

                    case JsonToken.Null:
                        reader.ReadNull();
                        break;

                    case JsonToken.None:
                        return;

                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }
}
