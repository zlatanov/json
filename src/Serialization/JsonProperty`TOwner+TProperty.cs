using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Maverick.Json.Serialization
{
    public class JsonProperty<TOwner, TProperty> : JsonProperty<TOwner>
    {
        public JsonProperty( JsonObjectContract<TOwner> parent, MemberInfo member ) : base( parent, member )
        {
            m_setter = CreateSetter( member );
            m_getter = ReflectionHelpers.CreateGetter<TOwner, TProperty, Getter>( member );

            if ( m_getter != null )
            {
                ShouldSerialize = CreateShouldSerialize( member );
            }

            CanSetValue = m_setter != null;
            CanGetValue = m_getter != null;

            Debug.Assert( PropertyType == typeof( TProperty ) );
        }


        public override Boolean CanSetValue { get; }


        public override Boolean CanGetValue { get; }


        internal override Int32 MemorySize { get; } = Unsafe.SizeOf<TProperty>();


        internal override Object GetValue( TOwner owner ) => m_getter( owner );


        internal override Object GetValue( ref JsonPropertyValues<TOwner> propertyValues ) => propertyValues.GetValue( this );


        protected internal override void WriteValue( JsonWriter writer, TOwner owner )
        {
            if ( ShouldSerialize != null && !ShouldSerialize( owner ) )
            {
                return;
            }

            var value = m_getter( owner );

            if ( value == null && !SerializeNulls )
            {
                return;
            }

            writer.WritePropertyName( Name );

            if ( Converter != null )
            {
                // Try to avoid boxing if possible
                if ( s_isValueType && Converter is JsonConverter<TProperty> valueConverter )
                {
                    valueConverter.Write( writer, value );
                }
                else
                {
                    Converter.WriteObject( writer, value );
                }
            }
            else
            {
                writer.WriteValue( value );
            }
        }


        internal override unsafe void ReadValue( JsonReader reader, ref TOwner target, ref JsonPropertyValues<TOwner> propertyValues )
        {
            TProperty value;

            if ( Converter != null )
            {
                // Try to avoid boxing if possible
                if ( s_isValueType && Converter is JsonConverter<TProperty> valueConverter )
                {
                    value = valueConverter.Read( reader, NonNullablePropertyType );
                }
                else
                {
                    value = (TProperty)Converter.ReadObject( reader, NonNullablePropertyType );
                }
            }
            else
            {
                // If what we are trying to read is reference type with already created 
                if ( m_setter == null && target != null && Traits<TProperty>.IsPopulatable )
                {
                    var currentValue = m_getter( target );

                    if ( currentValue != null )
                    {
                        reader.Populate( currentValue );
                        return;
                    }
                }

                value = reader.ReadValue<TProperty>();
            }

            propertyValues.SetValue( this, value );
        }


        internal override void SetValue( ref TOwner target, ref JsonPropertyValues<TOwner> propertyValues )
        {
            m_setter( ref target, propertyValues.GetValue( this ) );
        }


        internal override void SetValue( ref TOwner target, Object value ) => m_setter( ref target, (TProperty)value );


        internal override Boolean ValueEquals( Object left, Object right ) => EqualityComparer<TProperty>.Default.Equals( (TProperty)left, (TProperty)right );


        private static Setter CreateSetter( MemberInfo member )
        {
            // We cannot set init only (read only) fields
            if ( member is FieldInfo field && field.IsInitOnly )
            {
                return null;
            }

            // We need to get the ref TOwner type which is ByRef and the only way to do this is using reflection
            var target = Expression.Parameter( typeof( Setter ).GetMethod( "Invoke" ).GetParameters()[ 0 ].ParameterType, "target" );
            var setter = default( Expression );

            if ( member is PropertyInfo property )
            {
                if ( property.SetMethod != null )
                {
                    setter = Expression.Property( target, property.SetMethod );
                }
                else
                {
                    return null;
                }
            }
            else
            {
                setter = Expression.Field( target, (FieldInfo)member );
            }

            var value = Expression.Parameter( typeof( TProperty ), "value" );

            return Expression.Lambda<Setter>( Expression.Assign( setter, value ), target, value ).Compile();
        }


        private static Predicate<TOwner> CreateShouldSerialize( MemberInfo member )
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var method = typeof( TOwner ).GetMethod( "ShouldSerialize" + member.Name, flags, null, Type.EmptyTypes, null );

            if ( method != null && method.ReturnType == typeof( Boolean ) )
            {
                return (Predicate<TOwner>)method.CreateDelegate( typeof( Predicate<TOwner> ) );
            }

            return null;
        }


        private readonly Getter m_getter;
        private readonly Setter m_setter;

        private static readonly Boolean s_isValueType = typeof( TProperty ).IsValueType;

        private delegate TProperty Getter( TOwner target );
        private delegate void Setter( ref TOwner target, TProperty value );
    }
}
