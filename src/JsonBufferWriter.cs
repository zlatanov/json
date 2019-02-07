using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Maverick.Json
{
    public sealed class JsonBufferWriter : IBufferWriter<Byte>, IDisposable
    {
        public JsonBufferWriter() : this( 4096, ArrayPool<Byte>.Shared )
        {
        }


        public JsonBufferWriter( Int32 bufferSize ) : this( bufferSize, ArrayPool<Byte>.Shared )
        {
        }


        public JsonBufferWriter( Int32 bufferSize, ArrayPool<Byte> arrayPool )
        {
            m_arrayPool = arrayPool ?? throw new ArgumentNullException( nameof( arrayPool ) );
            m_current = arrayPool.Rent( bufferSize );
            m_available = m_current.Length;
        }


        public void Dispose()
        {
            if ( m_current != null )
            {
                m_arrayPool.Return( m_current );

                foreach ( var segment in m_segments )
                {
                    segment.Dispose();
                }

                m_available = 0;
                m_current = null;

                m_segments.Clear();
            }
        }


        public Byte[] ToArray() => ToSequence().ToArray();


        public void Advance( Int32 count )
        {
            if ( count > m_available )
            {
                throw new ArgumentOutOfRangeException( nameof( count ) );
            }

            m_offset += count;
            m_available -= count;
        }


        public Memory<Byte> GetMemory( Int32 sizeHint = 0 )
        {
            if ( sizeHint > m_available || m_available == 0 )
            {
                Resize( sizeHint );
            }

            return m_current.AsMemory( m_offset );
        }


        public Span<Byte> GetSpan( Int32 sizeHint = 0 )
        {
            if ( sizeHint > m_available || m_available == 0 )
            {
                Resize( sizeHint );
            }

            return m_current.AsSpan( m_offset );
        }


        public ReadOnlySequence<Byte> ToSequence()
        {
            if ( m_offset == 0 && m_segments.Count == 0 )
            {
                return ReadOnlySequence<Byte>.Empty;
            }

            Segment first, last;

            if ( m_offset > 0 )
            {
                last = new Segment( m_arrayPool, m_current, m_offset, m_segments.LastOrDefault() );
            }
            else
            {
                last = m_segments.Last();
            }

            first = m_segments.FirstOrDefault() ?? last;

            return new ReadOnlySequence<Byte>( first, 0, last, last.Memory.Length );
        }


        public void CopyTo( Stream stream )
        {
            foreach ( var segment in m_segments )
            {
                stream.Write( segment.Buffer, 0, segment.Count );
            }

            if ( m_offset > 0 )
            {
                stream.Write( m_current, 0, m_offset );
            }
        }


        public async Task CopyToAsync( Stream stream )
        {
            foreach ( var segment in m_segments )
            {
                await stream.WriteAsync( segment.Buffer, 0, segment.Count );
            }

            if ( m_offset > 0 )
            {
                await stream.WriteAsync( m_current, 0, m_offset );
            }
        }


        private void Resize( Int32 minimumSize )
        {
            var newBuffer = m_arrayPool.Rent( Math.Max( m_current.Length, minimumSize ) );

            if ( m_offset == 0 )
            {
                // Nothing has been written, we are just going to return the buffer
                m_arrayPool.Return( m_current );
            }
            else
            {
                // Store the current buffer - its offset is basically its length
                m_segments.Add( new Segment( m_arrayPool, m_current, m_offset, m_segments.LastOrDefault() ) );
            }

            m_current = newBuffer;
            m_available = newBuffer.Length;
            m_offset = 0;
        }


        private Byte[] m_current;
        private Int32 m_offset;
        private Int32 m_available;

        private readonly ArrayPool<Byte> m_arrayPool;
        private readonly List<Segment> m_segments = new List<Segment>();


        private sealed class Segment : ReadOnlySequenceSegment<Byte>, IDisposable
        {
            public Segment( ArrayPool<Byte> arrayPool, Byte[] buffer, Int32 count, Segment previous = null )
            {
                m_arrayPool = arrayPool;

                Count = count;
                Memory = new ReadOnlyMemory<Byte>( buffer, 0, count );

                if ( previous != null )
                {
                    previous.Next = this;
                    RunningIndex = previous.RunningIndex + previous.Memory.Length;
                }

                Buffer = buffer;
            }


            public Byte[] Buffer { get; private set; }
            public Int32 Count { get; }


            public void Dispose()
            {
                if ( Buffer != null )
                {
                    m_arrayPool.Return( Buffer );

                    Buffer = null;
                    Memory = default;
                }
            }


            private readonly ArrayPool<Byte> m_arrayPool;
        }
    }
}
