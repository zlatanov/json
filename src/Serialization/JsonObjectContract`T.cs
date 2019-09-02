using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Maverick.Json.Async;

namespace Maverick.Json.Serialization
{
    public sealed class JsonObjectContract<T> : JsonContract, IJsonContract<T>, IJsonPopulateFeature
    {
        internal JsonObjectContract( JsonSettings settings ) : base( typeof( T ) )
        {
            Settings = settings;
            Properties = new JsonPropertyCollection<T>( settings.NamingStrategy );
        }


        public JsonSettings Settings { get; }


        public JsonPropertyCollection<T> Properties { get; }


        public JsonConstructor<T> Constructor
        {
            get => m_ctor;
            set
            {
                CheckNotFrozen();

                m_ctor = value;
            }
        }


        protected internal override void Freeze()
        {
            Properties.Freeze();

            if ( Constructor == null )
            {
                Constructor = ObjectConstructor.Find( this );
            }

            base.Freeze();
        }


        void IJsonContract<T>.WriteValue( JsonWriter writer, T value ) => WriteValueCore( writer, value );


        T IJsonContract<T>.ReadValue( JsonReader reader, Type objectType ) => ReadValueCore( reader );


        unsafe Boolean IJsonPopulateFeature.Populate( JsonReader reader, Object targetObject )
        {
            // There is no reason to try and populate value types because they have copy semantics
            // and the work we do cannot be observed outside of this method.
            if ( Traits<T>.IsValueType )
            {
                return false;
            }

            if ( reader.Peek() == JsonToken.StartObject )
            {
                reader.ReadStartObject();
            }

            var target = (T)targetObject;
            var memory = stackalloc Byte[ Properties.ValueMemorySize ];
            var present = stackalloc Boolean[ Properties.Count ];
            var propertyValues = new JsonPropertyValues<T>( this, memory, present );

            try
            {
                while ( reader.Peek() == JsonToken.PropertyName )
                {
                    var property = reader.ReadPropertyName( Properties );

                    if ( property == null )
                    {
                        reader.Skip();
                        continue;
                    }
                    else if ( reader.Peek() == JsonToken.Null )
                    {
                        reader.ReadNull();
                        continue;
                    }

                    property.ReadValue( reader, ref target, ref propertyValues );
                }

                reader.ReadEndObject();
                propertyValues.CheckRequiredProperties();

                PopulateValues( ref target, ref propertyValues );
            }
            finally
            {
                propertyValues.Release();
            }

            return true;
        }


        Boolean IJsonPopulateFeature.Copy( Object source, Object target )
        {
            // There is no reason to try and populate value types because they have copy semantics
            // and the work we do cannot be observed outside of this method.
            if ( Traits<T>.IsValueType )
            {
                return false;
            }

            var source_ = (T)source;
            var target_ = (T)target;

            foreach ( var property in Properties.Sorted )
            {
                if ( property.CanGetValue )
                {
                    var sourceValue = property.GetValue( source_ );

                    if ( property.CanSetValue )
                    {
                        property.SetValue( ref target_, sourceValue );
                    }
                    else
                    {
                        CopyPropertyValue( target_, property, sourceValue );
                    }
                }
            }

            return true;
        }


        public override void WriteValue( JsonWriter writer, Object value ) => WriteValueCore( writer, (T)value );


        public override Task WriteValueAsync( JsonAsyncWriter writer, Object value ) => WriteValueCoreAsync( writer, (T)value );


        public override Object ReadValue( JsonReader reader, Type objectType ) => ReadValueCore( reader );


        private void WriteValueCore( JsonWriter writer, T value )
        {
            writer.WriteStartObject();

            foreach ( var property in Properties.SortedWritable )
            {
                property.WriteValue( writer, value );
            }

            writer.WriteEndObject();
        }


        private Task WriteValueCoreAsync( JsonAsyncWriter writer, T value )
        {
            writer.WriteStartObject();

            for ( var i = 0; i < Properties.SortedWritable.Length; ++i )
            {
                var property = Properties.SortedWritable[ i ];
                var task = property.WriteValueAsync( writer, value );

                if ( !task.IsCompleted )
                {
                    return ContinueWriteAsync( writer, value, task, i + 1 );
                }
            }

            writer.WriteEndObject();

            return Task.CompletedTask;
        }


        private async Task ContinueWriteAsync( JsonAsyncWriter writer, T value, Task task, Int32 startIndex )
        {
            await task;

            for ( var i = startIndex; i < Properties.SortedWritable.Length; ++i )
            {
                var property = Properties.SortedWritable[ i ];

                await property.WriteValueAsync( writer, value );
            }

            writer.WriteEndObject();
        }


        private unsafe T ReadValueCore( JsonReader reader )
        {
            if ( m_ctor == null )
            {
                throw new JsonSerializationException( $"Cannot create object instance for {UnderlyingType} because there is no suitable constructor available." );
            }

            var memory = stackalloc Byte[ Properties.ValueMemorySize ];
            var present = stackalloc Boolean[ Properties.Count ];
            var propertyValues = new JsonPropertyValues<T>( this, memory, present );

            var target = default( T );
            var targetCreated = false;

            try
            {
                reader.ReadStartObject();

                while ( reader.Peek() == JsonToken.PropertyName )
                {
                    var property = reader.ReadPropertyName( Properties );

                    if ( property == null )
                    {
                        reader.Skip();
                        continue;
                    }
                    else if ( reader.Peek() == JsonToken.Null )
                    {
                        reader.ReadNull();
                        continue;
                    }

                    if ( !targetCreated && m_ctor.CanExecute( ref propertyValues ) )
                    {
                        target = m_ctor.Factory( ref propertyValues );
                        targetCreated = true;
                    }

                    property.ReadValue( reader, ref target, ref propertyValues );
                }

                reader.ReadEndObject();
                propertyValues.CheckRequiredProperties();

                if ( !targetCreated )
                {
                    target = m_ctor.Factory( ref propertyValues );
                }

                PopulateValues( ref target, ref propertyValues );

                return target;
            }
            finally
            {
                propertyValues.Release();
            }
        }


        private void PopulateValues( ref T target, ref JsonPropertyValues<T> propertyValues )
        {
            foreach ( var property in Properties.Sorted )
            {
                if ( propertyValues.HasValue( property ) )
                {
                    if ( property.CanSetValue )
                    {
                        property.SetValue( ref target, ref propertyValues );
                    }
                    else
                    {
                        CopyPropertyValue( target, property, property.GetValue( ref propertyValues ) );
                    }
                }
            }
        }


        private void CopyPropertyValue( T target, JsonProperty<T> property, Object sourceValue )
        {
            Debug.Assert( property.CanSetValue == false );

            // The property is read only. Sice we've already deserialized the value
            // we must try to populate the existing value. If we fail to do so, unlike other JSON serializers,
            // we must throw exception.
            var targetValue = property.GetValue( target );
            var copied = true;

            if ( targetValue == null )
            {
                // We cannot copy the value if there is no target.
                // Succeed only if the source is null too.
                copied = sourceValue == null;
            }
            else if ( !property.ValueEquals( sourceValue, targetValue ) )
            {
                var propertyContract = Settings.ResolveContract( property.NonNullablePropertyType );

                copied = propertyContract is IJsonPopulateFeature feature
                      && feature.Copy( sourceValue, targetValue );
            }

            if ( !copied )
            {
                throw new JsonSerializationException( $"Property {property.UnderlyingName} in {property.DeclaringType} is read only and cannot be deserialized." );
            }
        }


        private JsonConstructor<T> m_ctor;
    }
}
