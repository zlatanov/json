using System;
using System.Buffers;
using System.Text;

namespace Maverick.Json
{
    internal static class BufferWriterExtensions
    {
        public static unsafe void WriteString( this IBufferWriter<Byte> writer, String value, Encoding encoding )
        {
            if ( value == null )
            {
                throw new ArgumentNullException( nameof( value ) );
            }
            else if ( value.Length == 0 )
            {
                return;
            }

            var encoder = encoding.GetEncoder();
            var chars = value.AsSpan();

            while ( true )
            {
                var span = writer.GetSpan( 6 );

                fixed ( Char* fixedChars = chars )
                fixed ( Byte* fixedBytes = span )
                {
                    encoder.Convert( chars: fixedChars,
                                     charCount: chars.Length,
                                     bytes: fixedBytes,
                                     byteCount: span.Length,
                                     flush: true,
                                     out var charsUsed,
                                     out var bytesUsed,
                                     out var completed );
                    writer.Advance( bytesUsed );

                    if ( completed )
                    {
                        break;
                    }

                    chars = chars.Slice( charsUsed );
                }
            }
        }
    }
}
