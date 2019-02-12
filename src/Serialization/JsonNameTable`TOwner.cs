using System;
using System.Collections.Generic;

namespace Maverick.Json.Serialization
{
    internal sealed class JsonNameTable<TOwner>
    {
        public JsonNameTable( JsonNamingStrategy namingStrategy )
        {
            m_namingStrategy = namingStrategy;
        }


        public unsafe JsonProperty<TOwner> Find( ReadOnlySpan<Byte> bytes )
        {
            fixed ( Byte* fixedBytes = bytes )
            {
                if ( m_properties.TryGetValue( new JsonNameKey( fixedBytes, bytes.Length ), out var value ) )
                {
                    return value;
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

            m_properties.Add( new JsonNameKey( bytes.ToArray() ), property );
        }


        private readonly JsonNamingStrategy m_namingStrategy;
        private readonly Dictionary<JsonNameKey, JsonProperty<TOwner>> m_properties = new Dictionary<JsonNameKey, JsonProperty<TOwner>>();


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
