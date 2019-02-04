using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Maverick.Json.Serialization;

namespace Maverick.Json
{
    /// <summary>
    ///  UTF-8 JSON reader that provides fast, non-cached, forward-only access to serialized JSON data.
    /// </summary>
    public class JsonReader
    {
        public JsonReader( ReadOnlySequence<Byte> sequence ) : this( sequence, JsonSettings.Default )
        {
        }


        public JsonReader( ReadOnlySequence<Byte> sequence, JsonSettings settings )
        {
            Settings = settings ?? throw new ArgumentNullException( nameof( settings ) );
            m_sequence = sequence;
            m_position = sequence.Start;
            m_memory = sequence.First;
        }


        public JsonSettings Settings { get; }


        public JsonToken Peek()
        {
            if ( m_currentToken == JsonToken.None )
            {
                m_currentToken = PeekCore();

                if ( m_expectComma )
                {
                    JsonSerializationException.ThrowMissingComma( m_currentToken );
                }
            }

            return m_currentToken;
        }


        /// <summary>
        /// Skips the children of the current token.
        /// </summary>
        public void Skip()
        {
            var token = Peek();

            switch ( token )
            {
                case JsonToken.PropertyName: SkipPropertyName(); break;
                case JsonToken.StartObject: SkipObject(); break;
                case JsonToken.StartArray: SkipArray(); break;
                case JsonToken.EndObject: ReadEndObject(); break;
                case JsonToken.EndArray: ReadEndArray(); break;
                case JsonToken.Number: SkipNumber(); break;
                case JsonToken.String: SkipString(); break;
                case JsonToken.Boolean: ReadBoolean(); break;
                case JsonToken.Null: ReadNull(); break;
                default:
                    throw new NotSupportedException( $"Unexpected JSON token {token}." );
            }

            void SkipPropertyName()
            {
                var byteCount = ReadStringByteCount( Constants.MaxPropertyNameSize, out var _, out var _ );
                CompleteReadPropertyName( byteCount );
            }

            void SkipObject()
            {
                ReadStartObject();
                while ( Peek() != JsonToken.EndObject )
                {
                    Skip();
                }
                ReadEndObject();
            }

            void SkipArray()
            {
                ReadStartArray();
                while ( Peek() != JsonToken.EndArray )
                {
                    Skip();
                }
                ReadEndArray();
            }

            void SkipNumber()
            {
                var byteCount = ReadValueByteCount( Constants.MaxFloatSize, out var _ );
                CompleteReadToken( JsonToken.Number, byteCount );
            }

            void SkipString()
            {
                var byteCount = ReadStringByteCount( Int32.MaxValue, out var _, out var _ );
                CompleteReadToken( JsonToken.String, byteCount + 1 );
            }
        }


        private void CheckToken( JsonToken token )
        {
            var nextToken = Peek();

            if ( nextToken != token )
            {
                throw new JsonSerializationException( $"Found {nextToken} where {token} was expected." );
            }
        }


        public void ReadStartObject()
        {
            CheckToken( JsonToken.StartObject );
            CompleteReadStartToken( JsonToken.StartObject );
        }


        public void ReadEndObject()
        {
            if ( m_state.Count == 0 || m_state.Peek() != JsonToken.StartObject )
            {
                JsonSerializationException.ThrowInvalidState( JsonToken.EndObject, m_state.Count == 0 ? JsonToken.None : m_state.Peek() );
            }

            CheckToken( JsonToken.EndObject );
            CompleteReadEndToken();
        }


        public void ReadStartArray()
        {
            CheckToken( JsonToken.StartArray );
            CompleteReadStartToken( JsonToken.StartArray );
        }


        public void ReadEndArray()
        {
            if ( m_state.Count == 0 || m_state.Peek() != JsonToken.StartArray )
            {
                JsonSerializationException.ThrowInvalidState( JsonToken.EndArray, m_state.Count == 0 ? JsonToken.None : m_state.Peek() );
            }

            CheckToken( JsonToken.EndArray );
            CompleteReadEndToken();
        }


        public String ReadPropertyName()
        {
            CheckToken( JsonToken.PropertyName );

            var byteCount = ReadStringByteCount( Constants.MaxPropertyNameSize, out var escapeByteCount, out var continuous );
            var propertyName = String.Empty;

            if ( byteCount != 0 )
            {
                if ( m_nameTable == null )
                {
                    m_nameTable = new JsonNameTable();
                }

                if ( continuous )
                {
                    propertyName = ReadPropertyName( GetSpan( byteCount ), escapeByteCount );
                }
                else
                {
                    Span<Byte> bytes = stackalloc Byte[ byteCount ];
                    CopySpan( bytes );

                    propertyName = ReadPropertyName( bytes, escapeByteCount );
                }
            }

            CompleteReadPropertyName( byteCount );

            return propertyName;
        }


        private String ReadPropertyName( ReadOnlySpan<Byte> buffer, Int32 escapeByteCount )
        {
            var propertyName = m_nameTable.Find( buffer );

            if ( propertyName == null )
            {
                propertyName = JsonPropertyName.RestoreCase( UnescapeString( buffer, escapeByteCount ), Settings.NamingStrategy );
                m_nameTable.Add( buffer, propertyName );
            }

            return propertyName;
        }


        public JsonProperty<TOwner> ReadPropertyName<TOwner>( JsonPropertyCollection<TOwner> properties )
        {
            CheckToken( JsonToken.PropertyName );

            var byteCount = ReadStringByteCount( Constants.MaxPropertyNameSize, out var escapeByteCount, out var continuous );
            var property = default( JsonProperty<TOwner> );

            if ( byteCount != 0 )
            {
                if ( continuous )
                {
                    property = properties.FindProperty( GetSpan( byteCount ) );
                }
                else
                {
                    Span<Byte> bytes = stackalloc Byte[ byteCount ];
                    CopySpan( bytes );

                    property = properties.FindProperty( bytes );
                }
            }

            CompleteReadPropertyName( byteCount );

            return property;
        }


        public Byte ReadByte() => checked((Byte)ReadUInt64());


        public Byte? ReadByteOrNull() => checked((Byte?)ReadUInt64OrNull());


        public SByte ReadSByte() => checked((SByte)ReadInt64());


        public SByte? ReadSByteOrNull() => checked((SByte?)ReadInt64OrNull());


        public Int16 ReadInt16() => checked((Int16)ReadInt64());


        public Int16? ReadInt16OrNull() => checked((Int16?)ReadInt64OrNull());


        public UInt16 ReadUInt16() => checked((UInt16)ReadUInt64());


        public UInt16? ReadUInt16OrNull() => checked((UInt16?)ReadUInt64OrNull());


        public Int32 ReadInt32() => checked((Int32)ReadInt64());


        public Int32? ReadInt32OrNull() => checked((Int32?)ReadInt64OrNull());


        public UInt32 ReadUInt32() => checked((UInt32)ReadUInt64());


        public UInt32? ReadUInt32OrNull() => checked((UInt32?)ReadUInt64OrNull());


        public Int64 ReadInt64()
        {
            CheckToken( JsonToken.Number );
            var byteCount = ReadValueByteCount( Constants.Max64BitNumberSize, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadInt64Core( bytes );
            }

            return ReadInt64Core( GetSpan( byteCount ) );
        }


        private Int64 ReadInt64Core( ReadOnlySpan<Byte> buffer )
        {
            if ( !Utf8Parser.TryParse( buffer, out Int64 value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( Int64 ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.Number, buffer.Length );

            return value;
        }


        public Int64? ReadInt64OrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadInt64();
        }


        public UInt64 ReadUInt64()
        {
            CheckToken( JsonToken.Number );
            var byteCount = ReadValueByteCount( Constants.Max64BitNumberSize, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadUInt64Core( bytes );
            }

            return ReadUInt64Core( GetSpan( byteCount ) );
        }


        private UInt64 ReadUInt64Core( ReadOnlySpan<Byte> buffer )
        {
            if ( !Utf8Parser.TryParse( buffer, out UInt64 value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( UInt64 ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.Number, buffer.Length );

            return value;
        }


        public UInt64? ReadUInt64OrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                return null;
            }

            return ReadUInt64();
        }


        public unsafe Char ReadChar()
        {
            CheckToken( JsonToken.String );
            var byteCount = ReadStringByteCount( Constants.MaxCharSize, out var escapeByteCount, out var continuous );

            if ( byteCount == 0 )
            {
                throw new JsonSerializationException( "Detected empty string while trying to read char." );
            }

            Span<Byte> bytes = stackalloc Byte[ byteCount ];
            CopySpan( bytes );

            var resultChar = default( Char );

            if ( bytes[ 0 ] == (Byte)'\\' )
            {
                switch ( bytes[ 1 ] )
                {
                    case (Byte)'"': resultChar = '"'; goto Complete;
                    case (Byte)'\\': resultChar = '\\'; goto Complete;
                    case (Byte)'/': resultChar = '/'; goto Complete;
                    case (Byte)'b': resultChar = '\b'; goto Complete;
                    case (Byte)'f': resultChar = '\f'; goto Complete;
                    case (Byte)'n': resultChar = '\n'; goto Complete;
                    case (Byte)'r': resultChar = '\r'; goto Complete;
                    case (Byte)'t': resultChar = '\t'; goto Complete;
                    case (Byte)'u':
                        if ( byteCount == 6 && Utf8Parser.TryParse( bytes.Slice( 2, 4 ), out Int32 value, out _, 'X' ) )
                        {
                            resultChar = (Char)value;
                            goto Complete;
                        }
                        break;
                }

                JsonSerializationException.ThrowInvalidValue( typeof( Char ), UnescapeString( bytes, escapeByteCount ) );
            }

            // Allocate 2 chars so we can catch when the encoded value has more than 1 char
            Span<Char> result = stackalloc Char[ 2 ];

            fixed ( Byte* fixedBytes = bytes )
            fixed ( Char* fixedResult = result )
            {
                var charCount = Constants.Encoding.GetChars( fixedBytes, byteCount, fixedResult, 2 );

                if ( charCount != 1 )
                {
                    JsonSerializationException.ThrowInvalidValue( typeof( Char ), UnescapeString( bytes, escapeByteCount ) );
                }
            }

            resultChar = result[ 0 ];

        Complete:
            CompleteReadToken( JsonToken.String, byteCount + 1 );

            return resultChar;
        }


        public Char? ReadCharOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadChar();
        }


        public String ReadString()
        {
            CheckToken( JsonToken.String );
            var byteCount = ReadStringByteCount( Int32.MaxValue, out var escapeByteCount, out var continuous );

            if ( byteCount == 0 )
            {
                CompleteReadToken( JsonToken.String, 1 );
                return String.Empty;
            }

            if ( !continuous )
            {
                var rentedBytes = ArrayPool<Byte>.Shared.Rent( byteCount );
                var buffer = rentedBytes.AsSpan( 0, byteCount );

                try
                {
                    CopySpan( buffer );

                    return ReadStringCore( buffer, escapeByteCount );
                }
                finally
                {
                    ArrayPool<Byte>.Shared.Return( rentedBytes );
                }
            }

            return ReadStringCore( GetSpan( byteCount ), escapeByteCount );
        }


        private unsafe String ReadStringCore( ReadOnlySpan<Byte> buffer, Int32 escapeByteCount )
        {
            String result;

            if ( escapeByteCount == 0 )
            {
                fixed ( Byte* fixedBuffer = buffer )
                {
                    result = Constants.Encoding.GetString( fixedBuffer, buffer.Length );
                }
            }
            else
            {
                result = UnescapeString( buffer, escapeByteCount );
            }

            CompleteReadToken( JsonToken.String, buffer.Length + 1 );

            return result;
        }


        public String ReadStringOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadString();
        }


        public Single ReadSingle()
        {
            if ( Peek() == JsonToken.String )
            {
                return ReadSingleSpecialValue();
            }

            CheckToken( JsonToken.Number );
            var byteCount = ReadValueByteCount( Constants.MaxFloatSize, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadSingleCore( bytes );
            }

            return ReadSingleCore( GetSpan( byteCount ) );
        }


        private Single ReadSingleCore( ReadOnlySpan<Byte> buffer )
        {
            if ( !Utf8Parser.TryParse( buffer, out Single value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( Single ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.Number, buffer.Length );

            return value;
        }


        public Single? ReadSingleOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadSingle();
        }


        public Double ReadDouble()
        {
            if ( Peek() == JsonToken.String )
            {
                return ReadDoubleSpecialValue();
            }

            CheckToken( JsonToken.Number );
            var byteCount = ReadValueByteCount( Constants.MaxFloatSize, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadDoubleCore( bytes );
            }

            return ReadDoubleCore( GetSpan( byteCount ) );
        }


        private Double ReadDoubleCore( ReadOnlySpan<Byte> buffer )
        {
            if ( !Utf8Parser.TryParse( buffer, out Double value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( Double ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.Number, buffer.Length );

            return value;
        }


        public Double? ReadDoubleOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadDouble();
        }


        public Decimal ReadDecimal()
        {
            CheckToken( JsonToken.Number );
            var byteCount = ReadValueByteCount( Constants.MaxFloatSize, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadDecimalCore( bytes );
            }

            return ReadDecimalCore( GetSpan( byteCount ) );
        }


        private Decimal ReadDecimalCore( ReadOnlySpan<Byte> buffer )
        {
            if ( !Utf8Parser.TryParse( buffer, out Decimal value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( Decimal ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.Number, buffer.Length );

            return value;
        }


        public Decimal? ReadDecimalOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadDecimal();
        }


        public Boolean ReadBoolean()
        {
            CheckToken( JsonToken.Boolean );

            var byteCount = ReadValueByteCount( Constants.MaxBooleanSize, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadBooleanCore( bytes );
            }

            return ReadBooleanCore( GetSpan( byteCount ) );
        }


        private Boolean ReadBooleanCore( ReadOnlySpan<Byte> buffer )
        {
            var value = false;

            if ( buffer[ 0 ] == (Byte)'t' )
            {
                if ( buffer[ 3 ] != (Byte)'e' || buffer[ 2 ] != (Byte)'u' || buffer[ 1 ] != (Byte)'r' )
                {
                    JsonSerializationException.ThrowInvalidValue( typeof( Boolean ), UnescapeString( buffer, 0 ) );
                }

                value = true;
            }
            else
            {
                // At this point the value can only be false
                if ( buffer.Length != 5 || buffer[ 4 ] != (Byte)'e' || buffer[ 3 ] != (Byte)'s' || buffer[ 2 ] != (Byte)'l' || buffer[ 1 ] != (Byte)'a' )
                {
                    JsonSerializationException.ThrowInvalidValue( typeof( Boolean ), UnescapeString( buffer, 0 ) );
                }
            }

            CompleteReadToken( JsonToken.Boolean, value ? 4 : 5 );

            return value;
        }


        public Boolean? ReadBooleanOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadBoolean();
        }


        public TimeSpan ReadTimeSpan()
        {
            CheckToken( JsonToken.String );
            var byteCount = ReadStringByteCount( Constants.MaxTimeSpanSize, out var _, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadTimeSpanCore( bytes );
            }

            return ReadTimeSpanCore( GetSpan( byteCount ) );
        }


        private TimeSpan ReadTimeSpanCore( ReadOnlySpan<Byte> buffer )
        {
            if ( !Utf8Parser.TryParse( buffer, out TimeSpan value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( TimeSpan ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.String, buffer.Length + 1 );

            return value;
        }


        public TimeSpan? ReadTimeSpanOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadTimeSpan();
        }


        public virtual DateTime ReadDateTime()
        {
            CheckToken( JsonToken.String );
            var byteCount = ReadStringByteCount( Constants.MaxDateTimeSize, out var _, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadDateTimeCore( bytes );
            }

            return ReadDateTimeCore( GetSpan( byteCount ) );
        }


        private DateTime ReadDateTimeCore( ReadOnlySpan<Byte> buffer )
        {
            if ( !DateTimeHelpers.TryParse( buffer, out DateTime value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( DateTime ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.String, buffer.Length + 1 );

            return value;
        }


        public DateTime? ReadDateTimeOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadDateTime();
        }


        public virtual DateTimeOffset ReadDateTimeOffset()
        {
            CheckToken( JsonToken.String );
            var byteCount = ReadStringByteCount( Constants.MaxDateTimeSize, out var _, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadDateTimeOffsetCore( bytes );
            }

            return ReadDateTimeOffsetCore( GetSpan( byteCount ) );
        }


        private DateTimeOffset ReadDateTimeOffsetCore( ReadOnlySpan<Byte> buffer )
        {
            if ( !DateTimeHelpers.TryParse( buffer, out DateTimeOffset value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( DateTimeOffset ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.String, buffer.Length + 1 );

            return value;
        }


        public DateTimeOffset? ReadDateTimeOffsetOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadDateTimeOffset();
        }


        public Guid ReadGuid()
        {
            CheckToken( JsonToken.String );
            var byteCount = ReadStringByteCount( Constants.MaxGuidSize, out var _, out var continuous );

            if ( !continuous )
            {
                Span<Byte> bytes = stackalloc Byte[ byteCount ];
                CopySpan( bytes );

                return ReadGuidCore( bytes );
            }

            return ReadGuidCore( GetSpan( byteCount ) );
        }


        private Guid ReadGuidCore( ReadOnlySpan<Byte> buffer )
        {
            if ( !Utf8Parser.TryParse( buffer, out Guid value, out var bytesConsumed ) || bytesConsumed != buffer.Length )
            {
                JsonSerializationException.ThrowInvalidValue( typeof( Guid ), UnescapeString( buffer, 0 ) );
            }

            CompleteReadToken( JsonToken.String, buffer.Length + 1 );

            return value;
        }


        public Guid? ReadGuidOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadGuid();
        }


        public Byte[] ReadByteArray()
        {
            CheckToken( JsonToken.String );
            var byteCount = ReadStringByteCount( Int32.MaxValue, out var _, out var _ );
            var rentedBytes = ArrayPool<Byte>.Shared.Rent( byteCount );

            CopySpan( rentedBytes.AsSpan( 0, byteCount ) );

            try
            {
                if ( Base64.DecodeFromUtf8InPlace( rentedBytes.AsSpan( 0, byteCount ), out var bytesWritten ) != OperationStatus.Done )
                {
                    throw new JsonSerializationException( "Detected invalid base64 string." );
                }

                CompleteReadToken( JsonToken.String, byteCount + 1 );

                return rentedBytes.AsSpan( 0, bytesWritten ).ToArray();
            }
            finally
            {
                ArrayPool<Byte>.Shared.Return( rentedBytes );
            }
        }


        public Byte[] ReadByteArrayOrNull()
        {
            if ( Peek() == JsonToken.Null )
            {
                CompleteReadNull();

                return null;
            }

            return ReadByteArray();
        }


        public Object ReadNull()
        {
            CheckToken( JsonToken.Null );
            CompleteReadNull();

            return null;
        }


        public T ReadValueIgnoreConverter<T>() => ReadValue<T>( true );


        public T ReadValue<T>() => ReadValue<T>( false );


        private T ReadValue<T>( Boolean ignoreConverter )
        {
            if ( Peek() == JsonToken.Null )
            {
                if ( Traits<T>.IsReferenceTypeOrNullable )
                {
                    CompleteReadNull();

                    return default;
                }

                throw new JsonSerializationException( $"Unexpected null when trying to read {typeof( T )}." );
            }

            if ( PrimitiveFormatter<T>.TryRead( this, out var value ) )
            {
                return value;
            }

            var objectType = ignoreConverter ? JsonIgnoreConverter.FromType( Traits<T>.NonNullableType ) : Traits<T>.NonNullableType;
            var contract = Settings.ResolveContract( objectType );

            return ReadValue<T>( contract );
        }


        private T ReadValue<T>( JsonContract contract )
        {
            if ( contract is IJsonContract<T> x )
            {
                return x.ReadValue( this, contract.UnderlyingType );
            }

            return (T)contract.ReadValue( this, contract.UnderlyingType );
        }


        internal T ReadValueInternal<T>( JsonContract contract )
        {
            if ( Peek() == JsonToken.Null )
            {
                if ( Traits<T>.IsReferenceTypeOrNullable )
                {
                    CompleteReadNull();

                    return default;
                }

                throw new JsonSerializationException( $"Unexpected null when trying to read {typeof( T )}." );
            }

            return ReadValue<T>( contract );
        }


        public Object ReadValue( Type valueType )
        {
            if ( Peek() == JsonToken.Null )
            {
                if ( valueType.IsClass || Nullable.GetUnderlyingType( valueType ) != null )
                {
                    CompleteReadNull();

                    return null;
                }

                throw new JsonSerializationException( $"Unexpected null when trying to read {valueType}." );
            }

            valueType = Nullable.GetUnderlyingType( valueType ) ?? valueType;

            return Settings
                .ResolveContract( valueType )
                .ReadValue( this, valueType );
        }


        public Object ReadValueIgnoreConverter( Type valueType ) => ReadValue( JsonIgnoreConverter.FromType( valueType ) );


        public void Populate( Object target )
        {
            if ( target == null )
            {
                throw new ArgumentNullException( nameof( target ) );
            }

            var contract = Settings.ResolveContract( target.GetType() );

            if ( contract is IJsonPopulateFeature feature )
            {
                if ( feature.Populate( this, target ) )
                {
                    return;
                }
            }

            throw new NotSupportedException( $"Object of type {target.GetType()} cannot be populated." );
        }


        private void ConsumeWhiteSpace()
        {
            while ( !m_memory.IsEmpty )
            {
                var consumedBytes = 0;
                var span = m_memory.Span;

                for ( var i = 0; i < span.Length; ++i )
                {
                    var b = span[ i ];
                    var token = Constants.AnsiTokenMap[ b ];

                    if ( token != JsonToken.WhiteSpace )
                    {
                        if ( consumedBytes > 0 )
                        {
                            Advance( consumedBytes );
                        }

                        return;
                    }

                    ++consumedBytes;

                    if ( b == Constants.LineFeed )
                    {
                        ++m_lineNumber;
                    }
                }

                Debug.Assert( consumedBytes == span.Length );
                Advance( consumedBytes );
            }
        }


        private JsonToken PeekCore()
        {
            while ( !m_memory.IsEmpty )
            {
                var span = m_memory.Span;
                var consumedByteCount = 0;

                for ( var i = 0; i < span.Length; ++i )
                {
                    var b = span[ i ];
                    var token = Constants.AnsiTokenMap[ b ];

                    switch ( token )
                    {
                        case JsonToken.EndObject:
                        case JsonToken.EndArray:
                            m_expectComma = false;
                            break;

                        case JsonToken.String:
                            ++consumedByteCount;
                            token = PeekStringOrPropertyName();
                            break;

                        case JsonToken.Null:
                            PeekNull( span, ref consumedByteCount, i );
                            break;

                        case JsonToken.Comma:
                            if ( !m_expectComma )
                            {
                                JsonSerializationException.ThrowUnexpectedSymbol( b );
                            }

                            m_expectComma = false;
                            goto Continue;

                        case JsonToken.WhiteSpace:
                            if ( b == Constants.LineFeed )
                            {
                                ++m_lineNumber;
                            }
                            goto Continue;

                        case JsonToken.None:
                            JsonSerializationException.ThrowUnexpectedSymbol( b );
                            break;
                    }

                    if ( consumedByteCount > 0 )
                    {
                        Advance( consumedByteCount );
                    }

                    return token;

                Continue:
                    ++consumedByteCount;
                    continue;
                }

                Debug.Assert( consumedByteCount == m_memory.Length );
                Advance( consumedByteCount );
            }

            return JsonToken.None;
        }


        private JsonToken PeekStringOrPropertyName()
        {
            if ( m_state.Count > 0 && m_state.Peek() == JsonToken.StartObject )
            {
                return JsonToken.PropertyName;
            }

            return JsonToken.String;
        }


        private void PeekNull( ReadOnlySpan<Byte> span, ref Int32 consumedByteCount, Int32 startIndex )
        {
            if ( !span.Slice( startIndex ).StartsWith( Constants.Null ) )
            {
                Advance( consumedByteCount );
                consumedByteCount = 0;

                Span<Byte> bytes = stackalloc Byte[ 4 ];
                CopySpan( bytes );

                if ( !bytes.SequenceEqual( Constants.Null ) )
                {
                    JsonSerializationException.ThrowUnexpectedSymbol( (Byte)'n' );
                }
            }
        }


        private void TryPopPropertyName()
        {
            if ( m_state.Count > 0 && m_state.Peek() == JsonToken.PropertyName )
            {
                m_state.Pop();
            }
        }


        private void CompleteReadNull()
        {
            Advance( 4 );
            TryPopPropertyName();

            m_expectComma = true;
            m_currentToken = JsonToken.None;
        }


        private void CompleteReadStartToken( JsonToken token )
        {
            Advance( 1 );
            m_state.Push( token );

            m_expectComma = false;
            m_currentToken = JsonToken.None;
        }


        private void CompleteReadToken( JsonToken token, Int32 bytesConsumed )
        {
            Advance( bytesConsumed );
            TryPopPropertyName();

            m_expectComma = true;
            m_currentToken = JsonToken.None;
        }


        private void CompleteReadEndToken()
        {
            Advance( 1 );
            m_state.Pop();
            TryPopPropertyName();

            m_expectComma = true;
            m_currentToken = JsonToken.None;
        }


        private void CompleteReadPropertyName( Int32 bytesConsumed )
        {
            Advance( bytesConsumed + 1 );
            m_state.Push( JsonToken.PropertyName );

            // Between the property name and the value there can only be white space
            // and exactly 1 symbol ':' serving as separator.
            if ( PeekByte() != (Byte)':' )
            {
                // Try to consume white space
                ConsumeWhiteSpace();

                if ( PeekByte() != (Byte)':' )
                {
                    JsonSerializationException.ThrowUnexpectedSymbol( PeekByte() );
                }
            }

            Advance( 1 );

            m_expectComma = false;
            m_currentToken = JsonToken.None;
        }


        [MethodImpl( MethodImplOptions.NoInlining )]
        private void ThrowObjectDisposedException() => throw new ObjectDisposedException( GetType().FullName );


        private Int32 ReadStringSegmentByteCount( ReadOnlySpan<Byte> buffer, ref Int32 escapeByteCount, out Boolean needMoreData )
        {
            needMoreData = true;

            var escaped = false;
            var index = 0;

            for ( ; index < buffer.Length; ++index )
            {
                var @byte = buffer[ index ];

                if ( escaped )
                {
                    if ( @byte == (Byte)'u' )
                    {
                        escapeByteCount += 4;
                        index += 4;
                    }

                    escaped = false;
                }
                else
                {
                    if ( @byte == (Byte)'"' )
                    {
                        needMoreData = false;
                        break;
                    }

                    if ( @byte == (Byte)'\\' )
                    {
                        ++escapeByteCount;
                        escaped = true;
                    }
                }
            }

            return index;
        }


        private Int32 ReadStringByteCount( Int32 maxSize, out Int32 escapeByteCount, out Boolean continuous )
        {
            escapeByteCount = 0;
            var count = ReadStringSegmentByteCount( m_memory.Span, ref escapeByteCount, out var needMoreData );

            if ( !needMoreData )
            {
                continuous = true;
            }
            else
            {
                continuous = false;
                var position = m_sequence.GetPosition( count, m_position );

                while ( needMoreData )
                {
                    if ( !m_sequence.TryGet( ref position, out var memory, advance: false ) || memory.Length == 0 )
                    {
                        JsonSerializationException.ThrowUnexpectedEnd();
                    }

                    var currentCount = ReadStringSegmentByteCount( memory.Span, ref escapeByteCount, out needMoreData );
                    position = m_sequence.GetPosition( currentCount, position );

                    count += currentCount;
                }
            }

            if ( count > maxSize )
            {
                JsonSerializationException.ThrowStringTooBig();
            }

            return count;
        }


        private Int32 ReadValueSegmentByteCount( ReadOnlySpan<Byte> buffer, out Boolean needMoreData )
        {
            var index = 0;

            for ( ; index < buffer.Length; ++index )
            {
                if ( Constants.AnsiValueSeparatorMap[ buffer[ index ] ] )
                {
                    break;
                }
            }

            needMoreData = index == buffer.Length;

            return index;
        }


        private Int32 ReadValueByteCount( Int32 maxSize, out Boolean continuous )
        {
            var count = ReadValueSegmentByteCount( m_memory.Span, out var needMoreData );

            if ( !needMoreData )
            {
                continuous = true;
            }
            else
            {
                continuous = false;
                var position = m_sequence.GetPosition( count, m_position );

                while ( needMoreData )
                {
                    if ( !m_sequence.TryGet( ref position, out var memory, advance: false ) )
                    {
                        JsonSerializationException.ThrowUnexpectedEnd();
                    }
                    else if ( memory.Length == 0 )
                    {
                        break;
                    }

                    var currentCount = ReadValueSegmentByteCount( memory.Span, out needMoreData );
                    position = m_sequence.GetPosition( currentCount, position );

                    count += currentCount;
                }
            }

            if ( count > maxSize )
            {
                JsonSerializationException.ThrowNumberTooBig();
            }

            return count;
        }


        private void Advance( Int32 byteCount )
        {
            if ( byteCount >= m_memory.Length )
            {
                m_position = m_sequence.GetPosition( byteCount, m_position );
                m_sequence.TryGet( ref m_position, out m_memory, advance: false );
            }
            else
            {
                m_position = new SequencePosition( m_position.GetObject(), m_position.GetInteger() + byteCount );
                m_memory = m_memory.Slice( byteCount );
            }
        }


        private ReadOnlySpan<Byte> GetSpan( Int32 size ) => m_memory.Span.Slice( 0, size );


        private void CopySpan( Span<Byte> bytes ) => m_sequence.Slice( m_position, bytes.Length ).CopyTo( bytes );


        private Byte PeekByte() => m_memory.IsEmpty ? Byte.MinValue : m_memory.Span[ 0 ];


        private unsafe String UnescapeString( ReadOnlySpan<Byte> span, Int32 escapeByteCount )
        {
            var rentedChars = ArrayPool<Char>.Shared.Rent( span.Length );

            try
            {
                Int32 escapedStringLength;

                fixed ( Byte* pBytes = span )
                fixed ( Char* pChars = rentedChars )
                {
                    escapedStringLength = Constants.Encoding.GetChars( pBytes, span.Length, pChars, rentedChars.Length );
                }

                var chars = rentedChars.AsSpan( 0, escapedStringLength );
                var result = new String( '\0', escapedStringLength - escapeByteCount );
                var resultIndex = 0;
                var previousCharIsEscape = false;

                fixed ( Char* pResult = result )
                {
                    for ( var i = 0; i < chars.Length; ++i )
                    {
                        var @char = chars[ i ];

                        if ( previousCharIsEscape )
                        {
                            previousCharIsEscape = false;
                            escapeByteCount -= 1;

                            switch ( @char )
                            {
                                case 'b': @char = '\b'; break;
                                case 'f': @char = '\f'; break;
                                case 'n': @char = '\n'; break;
                                case 'r': @char = '\r'; break;
                                case 't': @char = '\t'; break;
                                case 'u':
                                    escapeByteCount -= 4;
                                    @char = (Char)GetCodePoint( chars[ i + 1 ], chars[ i + 2 ], chars[ i + 3 ], chars[ i + 4 ] );
                                    i += 4;
                                    break;
                            }

                            *( pResult + resultIndex ) = @char;
                            ++resultIndex;

                            if ( escapeByteCount == 0 )
                            {
                                // Copy the rest of the chars if any
                                if ( i + 1 < chars.Length )
                                {
                                    chars.Slice( i + 1 ).CopyTo( new Span<Char>( pResult + resultIndex, result.Length - resultIndex ) );
                                }

                                break;
                            }
                        }
                        else
                        {
                            if ( @char == '\\' )
                            {
                                previousCharIsEscape = true;
                                continue;
                            }

                            *( pResult + resultIndex ) = @char;
                            ++resultIndex;
                        }
                    }
                }

                return result;
            }
            finally
            {
                ArrayPool<Char>.Shared.Return( rentedChars );
            }
        }


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private static Int32 GetCodePoint( Char a, Char b, Char c, Char d )
        {
            return ( ( ( ( ( ToNumber( a ) * 16 ) + ToNumber( b ) ) * 16 ) + ToNumber( c ) ) * 16 ) + ToNumber( d );
        }


        private static Int32 ToNumber( Char x )
        {
            if ( '0' <= x && x <= '9' )
            {
                return x - '0';
            }
            else if ( 'a' <= x && x <= 'f' )
            {
                return x - 'a' + 10;
            }
            else if ( 'A' <= x && x <= 'F' )
            {
                return x - 'A' + 10;
            }

            throw new JsonSerializationException( $"Invalid unicode code point character '{x}' encountered." );
        }


        private Single ReadSingleSpecialValue()
        {
            var byteCount = ReadStringByteCount( Constants.MaxFloatSize, out var escapeByteCount, out var continuous );
            Span<Byte> span = stackalloc Byte[ byteCount ];
            CopySpan( span );

            if ( span.SequenceEqual( Constants.NaN ) )
            {
                CompleteReadToken( JsonToken.String, Constants.NaN.Length + 1 );

                return Single.NaN;
            }
            else if ( span.SequenceEqual( Constants.NegativeInfinity ) )
            {
                CompleteReadToken( JsonToken.String, Constants.NegativeInfinity.Length + 1 );

                return Single.NegativeInfinity;
            }
            else if ( span.SequenceEqual( Constants.PositiveInfinity ) )
            {
                CompleteReadToken( JsonToken.String, Constants.PositiveInfinity.Length + 1 );

                return Single.PositiveInfinity;
            }

            var str = UnescapeString( span, escapeByteCount );

            throw new JsonSerializationException( $"Unexpected string value {str} while trying to deserialized float." );
        }


        private Double ReadDoubleSpecialValue()
        {
            var byteCount = ReadStringByteCount( Constants.MaxFloatSize, out var escapeByteCount, out var continuous );
            Span<Byte> span = stackalloc Byte[ byteCount ];
            CopySpan( span );

            if ( span.SequenceEqual( Constants.NaN ) )
            {
                CompleteReadToken( JsonToken.String, Constants.NaN.Length + 1 );

                return Double.NaN;
            }
            else if ( span.SequenceEqual( Constants.NegativeInfinity ) )
            {
                CompleteReadToken( JsonToken.String, Constants.NegativeInfinity.Length + 1 );

                return Double.NegativeInfinity;
            }
            else if ( span.SequenceEqual( Constants.PositiveInfinity ) )
            {
                CompleteReadToken( JsonToken.String, Constants.PositiveInfinity.Length + 1 );

                return Double.PositiveInfinity;
            }

            var str = UnescapeString( span, escapeByteCount );

            throw new JsonSerializationException( $"Unexpected string value {str} while trying to deserialized double." );
        }


        public override String ToString() => Constants.Encoding.GetString( m_sequence );


        private readonly ReadOnlySequence<Byte> m_sequence;

        private SequencePosition m_position;    // The current position of the reader
        private ReadOnlyMemory<Byte> m_memory;  // The memory at the current position

        private JsonToken m_currentToken = JsonToken.None;
        private Boolean m_expectComma;

        private readonly Stack<JsonToken> m_state = new Stack<JsonToken>();
        private JsonNameTable m_nameTable;

        private Int32 m_lineNumber;
    }
}
