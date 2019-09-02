using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Maverick.Json.Async;

namespace Maverick.Json
{
    public static class JsonConvert
    {
        private const Int32 BufferSize = 4096;


        public static String Serialize<T>( T value, JsonFormat? format = null, JsonSettings settings = null )
        {
            using ( var buffer = new JsonBufferWriter( BufferSize ) )
            {
                var writer = new JsonWriter( buffer, settings ?? JsonSettings.Default );

                if ( format != null )
                {
                    writer.Format = format.Value;
                }

                writer.WriteValue( value );

                return Constants.Encoding.GetString( buffer.Sequence );
            }
        }


        public static async Task<String> SerializeAsync<T>( T value, JsonSettings settings = null, JsonFormat? format = null )
        {
            using ( var buffer = new JsonBufferWriter( BufferSize ) )
            {
                var writer = new JsonAsyncWriter( buffer, settings ?? JsonSettings.Default );

                if ( format != null )
                {
                    writer.Format = format.Value;
                }

                await writer.WriteValueAsync( value );

                return Constants.Encoding.GetString( buffer.Sequence );
            }
        }


        public static void Serialize<T>( Stream stream, T value, JsonFormat? format = null, JsonSettings settings = null )
        {
            using ( var buffer = new JsonStreamWriter( stream, BufferSize ) )
            {
                var writer = new JsonWriter( buffer, settings ?? JsonSettings.Default );

                if ( format != null )
                {
                    writer.Format = format.Value;
                }

                writer.WriteValue( value );
            }
        }


        public static async Task SerializeAsync<T>( Stream stream, T value, JsonSettings settings = null, JsonFormat? format = null, CancellationToken cancellationToken = default )
        {
            using ( var buffer = new JsonAsyncStreamWriter( stream, BufferSize ) )
            {
                var writer = new JsonAsyncWriter( buffer, settings ?? JsonSettings.Default, cancellationToken );

                if ( format != null )
                {
                    writer.Format = format.Value;
                }

                await writer.WriteValueAsync( value ).ConfigureAwait( false );
                await buffer.FlushAsync( cancellationToken ).ConfigureAwait( false );
            }
        }


        public static T Deserialize<T>( String json, JsonSettings settings = null )
        {
            using ( var buffer = new JsonBufferWriter( BufferSize ) )
            {
                buffer.Write( json );
                var reader = new JsonReader( buffer.Sequence, settings ?? JsonSettings.Default );

                return reader.ReadValue<T>();
            }
        }


        public static T Deserialize<T>( Byte[] json, JsonSettings settings = null ) => Deserialize<T>( new ReadOnlySequence<Byte>( json ), settings );


        public static T Deserialize<T>( ReadOnlySequence<Byte> json, JsonSettings settings = null )
        {
            var reader = new JsonReader( json, settings ?? JsonSettings.Default );

            return reader.ReadValue<T>();
        }


        public static Object Deserialize( String json, Type objectType, JsonSettings settings = null )
        {
            using ( var buffer = new JsonBufferWriter( BufferSize ) )
            {
                buffer.Write( json );
                var reader = new JsonReader( buffer.Sequence, settings ?? JsonSettings.Default );

                return reader.ReadValue( objectType );
            }
        }


        public static T DeserializeAnonymous<T>( String json, T obj, JsonSettings settings = null ) => Deserialize<T>( json, settings );


        public static void Populate( String json, Object target, JsonSettings settings = null )
        {
            using ( var buffer = new JsonBufferWriter( BufferSize ) )
            {
                buffer.Write( json );
                var reader = new JsonReader( buffer.Sequence, settings ?? JsonSettings.Default );

                reader.Populate( target );
            }
        }
    }
}
