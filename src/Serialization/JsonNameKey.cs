using System;

namespace Maverick.Json.Serialization
{
    internal unsafe struct JsonNameKey : IEquatable<JsonNameKey>
    {
        internal JsonNameKey( Byte* bytes, Int32 length )
        {
            Bytes = null;

            UnsafeBytes = bytes;
            Length = length;
        }


        internal JsonNameKey( Byte[] bytes )
        {
            UnsafeBytes = null;

            Bytes = bytes;
            Length = bytes.Length;
        }


        public Boolean Equals( JsonNameKey other )
        {
            if ( other.Length != Length )
            {
                return false;
            }

            var left = Bytes != null ? new ReadOnlySpan<Byte>( Bytes ) : new ReadOnlySpan<Byte>( UnsafeBytes, Length );
            var right = other.Bytes != null ? new ReadOnlySpan<Byte>( other.Bytes ) : new ReadOnlySpan<Byte>( other.UnsafeBytes, other.Length );

            return left.SequenceEqual( right );
        }


        public override Int32 GetHashCode()
        {
            var hashCode = Length;
            var remainingLength = Length;

            if ( Bytes != null )
            {
                fixed ( Byte* fixedBytes = Bytes )
                {
                    ComputeHashCode( ref hashCode, fixedBytes );
                }
            }
            else
            {
                ComputeHashCode( ref hashCode, UnsafeBytes );
            }

            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;

            return hashCode;
        }


        private void ComputeHashCode( ref Int32 hashCode, Byte* bytes )
        {
            var remainingLength = Length;

            for ( var i = 0; i < Length; )
            {
                if ( remainingLength > 3 )
                {
                    hashCode += ( hashCode << 7 ) ^ ( *(Int32*)( bytes + i ) );
                    remainingLength -= 4;
                    i += 4;
                }
                else if ( remainingLength > 1 )
                {
                    hashCode += ( hashCode << 7 ) ^ ( *(Int16*)( bytes + i ) );
                    remainingLength -= 2;
                    i += 2;
                }
                else
                {
                    hashCode += ( hashCode << 7 ) ^ bytes[ i ];
                    remainingLength -= 1;
                    i += 1;
                }
            }
        }


        public override Boolean Equals( Object obj ) => throw new NotSupportedException();


        public readonly Byte* UnsafeBytes;
        public readonly Int32 Length;

        public readonly Byte[] Bytes;
    }
}
