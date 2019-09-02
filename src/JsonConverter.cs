using System;
using System.Threading.Tasks;
using Maverick.Json.Async;

namespace Maverick.Json
{
    public abstract class JsonConverter
    {
        public abstract Boolean CanConvert( Type type );


        public abstract void WriteObject( JsonWriter writer, Object value );


        public virtual Task WriteObjectAsync( JsonAsyncWriter writer, Object value )
        {
            WriteObject( writer, value );

            return Task.CompletedTask;
        }


        public abstract Object ReadObject( JsonReader reader, Type objectType );
    }
}
