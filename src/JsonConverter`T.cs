using System;
using System.Threading.Tasks;
using Maverick.Json.Async;

namespace Maverick.Json
{
    public abstract class JsonConverter<T> : JsonConverter
    {
        public override Boolean CanConvert( Type type ) => typeof( T ).IsAssignableFrom( type );


        public override sealed void WriteObject( JsonWriter writer, Object value ) => Write( writer, (T)value );


        public override sealed Task WriteObjectAsync( JsonAsyncWriter writer, Object value ) => WriteAsync( writer, (T)value );


        public override sealed Object ReadObject( JsonReader reader, Type objectType ) => Read( reader, objectType );


        public abstract void Write( JsonWriter writer, T value );


        public virtual Task WriteAsync( JsonAsyncWriter writer, T value )
        {
            Write( writer, value );

            return Task.CompletedTask;
        }


        public abstract T Read( JsonReader reader, Type objectType );
    }
}
