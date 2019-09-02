using System.Threading.Tasks;
using Maverick.Json.Async;

namespace Maverick.Json.Converters
{
    internal sealed class CollectionNonAllocatingConverter<TCollection, TItem, TEnumerator> : CollectionConverter<TCollection, TItem>
        where TEnumerator : struct
    {
        public override void Write( JsonWriter writer, TCollection value )
        {
            writer.WriteStartArray();

            var enumerator = new Enumerator<TCollection, TItem, TEnumerator>( value );

            while ( enumerator.MoveNext() )
            {
                writer.WriteValue( enumerator.GetCurrent() );
            }

            writer.WriteEndArray();
        }


        public override Task WriteAsync( JsonAsyncWriter writer, TCollection value )
        {
            var task = writer.WriteStartArrayAsync();
            var enumerator = new Enumerator<TCollection, TItem, TEnumerator>( value );

            if ( !task.IsCompleted )
                return WriteAsync( writer, enumerator, task );

            while ( enumerator.MoveNext() )
            {
                task = writer.WriteValueAsync( enumerator.GetCurrent() );

                if ( !task.IsCompleted )
                    return WriteAsync( writer, enumerator, task );
            }

            return writer.WriteEndArrayAsync();
        }


        private static async Task WriteAsync( JsonAsyncWriter writer,
                                              Enumerator<TCollection, TItem, TEnumerator> enumerator,
                                              Task pendingTask )
        {
            await pendingTask;

            while ( enumerator.MoveNext() )
            {
                await writer.WriteValueAsync( enumerator.GetCurrent() );
            }

            await writer.WriteEndArrayAsync();
        }
    }
}
