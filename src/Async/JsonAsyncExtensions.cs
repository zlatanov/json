using System;
using System.Buffers;

namespace Maverick.Json.Async
{
    public static class JsonAsyncExtensions
    {
        public static Boolean TryGetSpan( this IBufferWriter<Byte> writer, Int32 length, out Span<Byte> span )
        {
            var buffer = writer.GetSpan();

            if ( buffer.Length >= length )
            {
                span = buffer;
                return true;
            }

            span = default;
            return false;
        }
    }
}
