using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Maverick.Json.Async;

namespace Maverick.Json
{
    internal sealed class PrimitiveFormatter<T>
    {
        static PrimitiveFormatter()
        {
            var type = typeof( T );
            var nonNullableType = Nullable.GetUnderlyingType( type ) ?? type;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            MethodInfo writeMethod, writeAsyncMethod, readMethod = null;

            if ( nonNullableType.IsEnum )
            {
                var methodName = nonNullableType != type ? nameof( JsonWriter.WriteEnumNullable ) : nameof( JsonWriter.WriteEnum );

                writeMethod = typeof( JsonWriter ).GetMethod( methodName, flags ).MakeGenericMethod( nonNullableType );
                writeAsyncMethod = typeof( JsonAsyncWriter ).GetMethod( methodName + "Async", flags )?.MakeGenericMethod( nonNullableType );

                readMethod = null;
            }
            else
            {
                writeMethod = typeof( JsonWriter ).GetMethod( nameof( JsonWriter.WriteValue ), flags, null, new Type[] { type }, null );
                writeAsyncMethod = typeof( JsonAsyncWriter ).GetMethod( nameof( JsonAsyncWriter.WriteValueAsync ), flags, null, new Type[] { type }, null );

                if ( writeMethod != null )
                {
                    readMethod = typeof( JsonReader ).GetMethod( GetReadMethodName( type ), flags );
                }
            }

            if ( writeMethod != null )
            {
                m_write = (Action<JsonWriter, T>)writeMethod.CreateDelegate( typeof( Action<JsonWriter, T> ) );
                m_writeAsync = (Func<JsonAsyncWriter, T, Task>)writeAsyncMethod?.CreateDelegate( typeof( Func<JsonAsyncWriter, T, Task> ) );

                if ( readMethod != null )
                {
                    m_read = (Func<JsonReader, T>)readMethod.CreateDelegate( typeof( Func<JsonReader, T> ) );
                    CanRead = true;
                }
            }
        }


        public static readonly Boolean CanRead;


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static Boolean TryWrite( JsonWriter writer, T value )
        {
            if ( m_write != null )
            {
                m_write( writer, value );

                return true;
            }

            return false;
        }


        public static Boolean TryWriteAsync( JsonAsyncWriter writer, T value, out Task task )
        {
            if ( m_writeAsync != null )
            {
                task = m_writeAsync( writer, value );
                return true;
            }

            if ( m_write != null )
            {
                m_write( writer, value );
                task = Task.CompletedTask;
                return true;
            }

            task = null;
            return false;
        }


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static Boolean TryRead( JsonReader reader, out T value )
        {
            if ( m_read != null )
            {
                value = m_read( reader );
                return true;
            }

            value = default;
            return false;
        }


        private static String GetReadMethodName( Type type )
        {
            if ( type == typeof( Byte[] ) )
                return nameof( JsonReader.ReadByteArray );

            var nonNullableType = Nullable.GetUnderlyingType( type ) ?? type;

            if ( type != nonNullableType )
            {
                return $"Read{nonNullableType.Name}OrNull";
            }

            return "Read" + type.Name;
        }


        private static readonly Action<JsonWriter, T> m_write;
        private static readonly Func<JsonAsyncWriter, T, Task> m_writeAsync;

        private static readonly Func<JsonReader, T> m_read;
    }
}
