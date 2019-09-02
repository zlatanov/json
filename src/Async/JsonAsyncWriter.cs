using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Maverick.Json.Serialization;

namespace Maverick.Json.Async
{
    public class JsonAsyncWriter : JsonWriter
    {
        public JsonAsyncWriter( IBufferWriter<Byte> output, CancellationToken cancellationToken = default )
            : this( output, JsonSettings.Default, cancellationToken )
        {
        }


        public JsonAsyncWriter( IBufferWriter<Byte> output, JsonSettings settings, CancellationToken cancellationToken = default )
            : base( output, settings )
        {
            CancellationToken = cancellationToken;
        }


        public CancellationToken CancellationToken { get; }


        public Task WriteStartArrayAsync()
        {
            WriteStarted( InternalState.Start );
            Depth += 1;

            return WriteUTFByteAsync( (Byte)'[' );
        }


        public Task WriteEndArrayAsync()
        {
            if ( Depth == 0 )
                throw new JsonSerializationException( "Cannot write end array because the current depth is 0." );

            Depth -= 1;
            WriteStarted( InternalState.End );

            return WriteUTFByteAsync( (Byte)']' );
        }


        public Task WritePropertyNameAsync( JsonPropertyName name )
        {
            if ( name is null )
                throw new ArgumentNullException( nameof( name ) );

            WriteStarted( InternalState.PropertyName );

            return WriteUTFBytesAsync( name.GetBytes( Settings.NamingStrategy ) );
        }


        public Task WriteNullAsync()
        {
            WriteStarted( InternalState.Value );

            var span = m_output.GetSpan();

            if ( span.Length < 4 )
            {
                return FlushNullAsync();
            }

            span[ 3 ] = (Byte)'l';
            span[ 2 ] = (Byte)'l';
            span[ 1 ] = (Byte)'u';
            span[ 0 ] = (Byte)'n';

            m_output.Advance( 4 );

            return Task.CompletedTask;
        }


        private async Task FlushNullAsync()
        {
            await FlushAsync();

            WriteNull();
        }


        public Task WriteValueAsync( Byte value ) => WriteValueAsync( (UInt64)value );


        public Task WriteValueAsync( Byte? value )
        {
            if ( value == null )
            {
                return WriteNullAsync();
            }

            return WriteValueAsync( value.Value );
        }


        public Task WriteValueAsync( SByte value ) => WriteValueAsync( (Int64)value );


        public Task WriteValueAsync( SByte? value )
        {
            if ( value == null )
            {
                return WriteNullAsync();
            }

            return WriteValueAsync( value.Value );
        }


        public Task WriteValueAsync( Int16 value ) => WriteValueAsync( (Int64)value );


        public Task WriteValueAsync( Int16? value )
        {
            if ( value == null )
            {
                return WriteNullAsync();
            }

            return WriteValueAsync( value.Value );
        }


        public Task WriteValueAsync( UInt16 value ) => WriteValueAsync( (UInt64)value );


        public Task WriteValueAsync( UInt16? value )
        {
            if ( value == null )
            {
                return WriteNullAsync();
            }

            return WriteValueAsync( value.Value );
        }


        public Task WriteValueAsync( Int32 value ) => WriteValueAsync( (Int64)value );


        public Task WriteValueAsync( Int32? value )
        {
            if ( value == null )
            {
                return WriteNullAsync();
            }

            return WriteValueAsync( value.Value );
        }


        public Task WriteValueAsync( UInt32 value ) => WriteValueAsync( (UInt64)value );


        public Task WriteValueAsync( UInt32? value )
        {
            if ( value == null )
            {
                return WriteNullAsync();
            }

            return WriteValueAsync( value.Value );
        }


        public Task WriteValueAsync( Int64 value )
        {
            WriteStarted( InternalState.Value );

            if ( !Utf8Formatter.TryFormat( value, m_output.GetSpan(), out var bytesWritten ) )
            {
                return FlushValueAsync( value );
            }

            m_output.Advance( bytesWritten );

            return Task.CompletedTask;
        }


        private async Task FlushValueAsync( Int64 value )
        {
            await FlushAsync();

            if ( !Utf8Formatter.TryFormat( value, m_output.GetSpan( Constants.Max64BitNumberSize ), out var bytesWritten ) )
            {
                ThrowFormatException( value );
            }

            m_output.Advance( bytesWritten );
        }


        public Task WriteValueAsync( Int64? value )
        {
            if ( value == null )
            {
                return WriteNullAsync();
            }

            return WriteValueAsync( value.Value );
        }


        public Task WriteValueAsync( UInt64 value )
        {
            WriteStarted( InternalState.Value );

            if ( !Utf8Formatter.TryFormat( value, m_output.GetSpan(), out var bytesWritten ) )
            {
                return FlushValueAsync( value );
            }

            m_output.Advance( bytesWritten );

            return Task.CompletedTask;
        }


        public async Task FlushValueAsync( UInt64 value )
        {
            await FlushAsync();

            if ( !Utf8Formatter.TryFormat( value, m_output.GetSpan( Constants.Max64BitNumberSize ), out var bytesWritten ) )
            {
                ThrowFormatException( value );
            }

            m_output.Advance( bytesWritten );
        }


        public Task WriteValueAsync( UInt64? value )
        {
            if ( value == null )
            {
                return WriteNullAsync();
            }

            return WriteValueAsync( value.Value );
        }


        public Task WriteValueAsync<T>( T value ) => WriteValueAsync( value, false );


        private Task WriteValueAsync<T>( T value, Boolean ignoreConverter )
        {
            if ( PrimitiveFormatter<T>.TryWriteAsync( this, value, out var primitiveTask ) )
                return primitiveTask;

            if ( value == null )
                return WriteNullAsync();

            var contractType = value.GetType();

            if ( ignoreConverter )
            {
                contractType = JsonIgnoreConverter.FromType( contractType );
            }

            var contract = Settings.ResolveContract( contractType );

            // If the object we try to write is value type then try to see if the contract
            // has implemented IJsonContract<T> so we can avoid memory allocation of boxing the value.
            if ( Traits<T>.IsValueType && contract is IJsonContract<T> objectContract )
            {
                objectContract.WriteValue( this, value );
            }
            else
            {
                var removeCircularReference = false;

                // Guard against circular references after reasonable depth
                // so we don't incur unnecessary performance penalty from premature
                // guarding against such.
                if ( Depth > 10 )
                {
                    if ( m_circularReferences == null )
                    {
                        m_circularReferences = new List<Object>();
                    }

                    if ( m_circularReferences.Contains( value ) )
                    {
                        throw new JsonSerializationException( $"Self referencing loop detected with {value} detected." );
                    }

                    m_circularReferences.Add( value );
                    removeCircularReference = true;
                }

                var task = contract.WriteValueAsync( this, value );

                if ( !task.IsCompleted && removeCircularReference )
                {
                    return RemoveCircularReferenceWhenTaskCompletesAsync( task, value );
                }
                else if ( removeCircularReference )
                {
                    m_circularReferences.Remove( value );
                }

                return task;
            }

            return Task.CompletedTask;
        }


        private async Task RemoveCircularReferenceWhenTaskCompletesAsync( Task task, Object value )
        {
            await task;

            m_circularReferences.Remove( value );
        }


        private Task WriteUTFByteAsync( Byte value )
        {
            var span = m_output.GetSpan();

            if ( span.Length == 0 )
            {
                return FlushUTFByteAsync( value );
            }

            span[ 0 ] = value;
            m_output.Advance( 1 );

            return Task.CompletedTask;
        }


        private async Task FlushUTFByteAsync( Byte value )
        {
            await FlushAsync();

            m_output.GetSpan( 1 )[ 0 ] = value;
            m_output.Advance( 1 );
        }


        private Task WriteUTFBytesAsync( ReadOnlyMemory<Byte> bytes, Boolean useHint = false )
        {
            var target = m_output.GetSpan();

            if ( target.Length == 0 && useHint )
            {
                target = m_output.GetSpan( bytes.Length );
            }

            if ( target.Length >= bytes.Length )
            {
                bytes.Span.CopyTo( target );
                m_output.Advance( bytes.Length );

                return Task.CompletedTask;
            }

            if ( target.Length > 0 )
            {
                bytes.Slice( 0, target.Length ).Span.CopyTo( target );
                m_output.Advance( target.Length );

                bytes = bytes.Slice( start: bytes.Length - target.Length );
            }

            return FlushUTFBytesAsync( bytes );
        }


        private async Task FlushUTFBytesAsync( ReadOnlyMemory<Byte> bytes )
        {
            await FlushAsync();

            if ( bytes.Length > 0 )
            {
                await WriteUTFBytesAsync( bytes, useHint: true );
            }
        }


        private Task FlushAsync()
        {
            if ( m_output is IAsyncOutput output )
            {
                return output.FlushAsync( CancellationToken );
            }

            return Task.CompletedTask;
        }
    }
}
