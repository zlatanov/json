using System;

namespace Maverick.Json
{
    public abstract class JsonConverter
    {
        public virtual bool HandleNull => false;


        public abstract Boolean CanConvert( Type type );


        public abstract void WriteObject( JsonWriter writer, Object value );


        public abstract Object ReadObject( JsonReader reader, Type objectType );
    }
}
