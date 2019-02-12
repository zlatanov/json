using System;
using System.Collections.Generic;

namespace Maverick.Json.Serialization
{
    internal sealed class JsonNameTable
    {
        public unsafe String Find( ReadOnlySpan<Byte> bytes )
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


        public void Add( ReadOnlySpan<Byte> bytes, String value )
        {
            m_properties.Add( new JsonNameKey( bytes.ToArray() ), value );
        }


        private readonly Dictionary<JsonNameKey, String> m_properties = new Dictionary<JsonNameKey, String>();
    }
}
