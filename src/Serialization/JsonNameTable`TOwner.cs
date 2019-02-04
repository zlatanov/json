using System;

namespace Maverick.Json.Serialization
{
    internal sealed class JsonNameTable<TOwner>
    {
        public JsonNameTable( JsonNamingStrategy namingStrategy )
        {
            m_namingStrategy = namingStrategy;
            m_entries = new Entry[ m_mask + 1 ];
        }


        public JsonProperty<TOwner> Find( ReadOnlySpan<Byte> bytes )
        {
            var hashCode = JsonNameTable.ComputeHash( bytes );

            for ( var entry = m_entries[ hashCode & m_mask ]; entry != null; entry = entry.Next )
            {
                if ( entry.HashCode == hashCode && bytes.SequenceEqual( entry.ValueBytes ) )
                {
                    return entry.Property;
                }
            }

            return null;
        }


        public void Add( JsonProperty<TOwner> property )
        {
            var bytes = property.Name.GetBytesNoQuotes( m_namingStrategy );

            // It is possible for more than one property to have the same byte representation
            // in a given naming strategy. In this case we honor the first property.
            if ( Find( bytes ) != null )
            {
                return;
            }

            var hashCode = JsonNameTable.ComputeHash( bytes );
            var index = hashCode & m_mask;
            var entry = new Entry( property, bytes.ToArray(), hashCode, m_entries[ index ] );

            m_entries[ index ] = entry;

            if ( m_count++ == m_mask )
            {
                Grow();
            }
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


        private readonly JsonNamingStrategy m_namingStrategy;

        private Int32 m_count;
        private Entry[] m_entries;
        private Int32 m_mask = 31;


        private sealed class Entry
        {
            public Entry( JsonProperty<TOwner> value, Byte[] bytes, Int32 hashCode, Entry next )
            {
                Property = value;
                ValueBytes = bytes;
                HashCode = hashCode;
                Next = next;
            }


            public JsonProperty<TOwner> Property { get; }
            public Byte[] ValueBytes { get; }
            public Int32 HashCode { get; }
            public Entry Next { get; set; }


            public override String ToString() => Property.Name.Value;
        }
    }
}
