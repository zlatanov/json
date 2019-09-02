using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Maverick.Json.Async
{
    public sealed class JsonAsyncStreamWriter : IAsyncOutput, IDisposable
    {
        public JsonAsyncStreamWriter( Stream stream ) : this( stream, 4096, ArrayPool<Byte>.Shared )
        {
        }


        public JsonAsyncStreamWriter( Stream stream, Int32 bufferSize ) : this( stream, bufferSize, ArrayPool<Byte>.Shared )
        {
        }


        public JsonAsyncStreamWriter( Stream stream, Int32 bufferSize, ArrayPool<Byte> arrayPool )
        {
            m_arrayPool = arrayPool ?? throw new ArgumentNullException( nameof( arrayPool ) );
            m_stream = stream ?? throw new ArgumentNullException( nameof( stream ) );
            m_buffer = arrayPool.Rent( bufferSize );
            m_available = m_buffer.Length;
        }


        public void Dispose()
        {
            if ( m_buffer != null )
            {
                if ( m_offset > 0 )
                {
                    throw new InvalidOperationException( "You must call FlushAsync before calling Dispose." );
                }

                m_arrayPool.Return( m_buffer );

                m_buffer = null;
                m_stream = null;
            }
        }


        public async Task FlushAsync( CancellationToken cancellationToken = default )
        {
            if ( m_offset > 0 )
            {
                await m_stream.WriteAsync( m_buffer, 0, m_offset, cancellationToken );

                m_offset = 0;
                m_available = m_buffer.Length;
            }
        }


        public void Advance( Int32 count )
        {
            if ( count > m_available )
            {
                throw new ArgumentOutOfRangeException( nameof( count ) );
            }

            m_offset += count;
            m_available -= count;
        }


        public Memory<Byte> GetMemory( Int32 sizeHint )
        {
            if ( sizeHint > m_available || m_available == 0 )
            {
                if ( sizeHint > 0 )
                {
                    var newBuffer = m_arrayPool.Rent( m_offset + sizeHint );

                    if ( m_offset > 0 )
                    {
                        m_buffer.AsSpan( 0, m_offset ).CopyTo( newBuffer );
                    }

                    m_arrayPool.Return( m_buffer );
                    m_buffer = newBuffer;
                    m_available = newBuffer.Length - m_offset;
                }
            }

            return m_buffer.AsMemory( m_offset );
        }


        public Span<Byte> GetSpan( Int32 sizeHint )
        {
            if ( sizeHint > m_available || m_available == 0 )
            {
                if ( sizeHint > 0 )
                {
                    var newBuffer = m_arrayPool.Rent( m_offset + sizeHint );

                    if ( m_offset > 0 )
                    {
                        m_buffer.AsSpan( 0, m_offset ).CopyTo( newBuffer );
                    }

                    m_arrayPool.Return( m_buffer );
                    m_buffer = newBuffer;
                    m_available = newBuffer.Length - m_offset;
                }
            }

            return m_buffer.AsSpan( m_offset );
        }


        private readonly ArrayPool<Byte> m_arrayPool;
        private Stream m_stream;

        private Byte[] m_buffer;
        private Int32 m_offset;
        private Int32 m_available;
    }
}
