using System;
using System.Runtime.CompilerServices;

namespace Maverick.Json.Serialization
{
    internal sealed class JsonNameTable
    {
        public JsonNameTable()
        {
            m_entries = new Entry[ m_mask + 1 ];
        }


        public String Find( ReadOnlySpan<Byte> bytes )
        {
            var hashCode = ComputeHash( bytes );

            for ( var entry = m_entries[ hashCode & m_mask ]; entry != null; entry = entry.Next )
            {
                if ( entry.HashCode == hashCode && bytes.SequenceEqual( entry.ValueBytes ) )
                {
                    return entry.Value;
                }
            }

            return null;
        }


        public void Add( ReadOnlySpan<Byte> bytes, String value )
        {
            var hashCode = ComputeHash( bytes );
            var index = hashCode & m_mask;
            var entry = new Entry( value, bytes.ToArray(), hashCode, m_entries[ index ] );

            m_entries[ index ] = entry;

            if ( m_count++ == m_mask )
            {
                Grow();
            }
        }


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        internal static unsafe Int32 ComputeHash( ReadOnlySpan<Byte> bytes )
        {
            var hashCode = bytes.Length;
            var remainingLength = bytes.Length;

            fixed ( Byte* pBytes = bytes )
            {
                for ( var i = 0; i < bytes.Length; )
                {
                    if ( remainingLength > 3 )
                    {
                        hashCode += ( hashCode << 7 ) ^ ( *(Int32*)( pBytes + i ) );
                        remainingLength -= 4;
                        i += 4;
                    }
                    else if ( remainingLength > 1 )
                    {
                        hashCode += ( hashCode << 7 ) ^ ( *(Int16*)( pBytes + i ) );
                        remainingLength -= 2;
                        i += 2;
                    }
                    else
                    {
                        hashCode += ( hashCode << 7 ) ^ pBytes[ i ];
                        remainingLength -= 1;
                        i += 1;
                    }
                }
            }

            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;

            return hashCode;
        }


        private void Grow()
        {
            var entries = m_entries;
            var newMask = ( m_mask * 2 ) + 1;
            var newEntries = new Entry[ newMask + 1 ];

            for ( var i = 0; i < entries.Length; i++ )
            {
                Entry next;

                for ( var entry = entries[ i ]; entry != null; entry = next )
                {
                    var index = entry.HashCode & newMask;

                    next = entry.Next;
                    entry.Next = newEntries[ index ];

                    newEntries[ index ] = entry;
                }
            }

            m_entries = newEntries;
            m_mask = newMask;
        }



        private Int32 m_count;
        private Entry[] m_entries;
        private Int32 m_mask = 31;


        private sealed class Entry
        {
            public Entry( String value, Byte[] bytes, Int32 hashCode, Entry next )
            {
                Value = value;
                ValueBytes = bytes;
                HashCode = hashCode;
                Next = next;
            }


            public readonly String Value;
            public readonly Byte[] ValueBytes;
            public readonly Int32 HashCode;
            public Entry Next;


            public override String ToString() => Value;
        }
    }
}
