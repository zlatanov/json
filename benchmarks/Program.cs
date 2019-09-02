using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Maverick.Json.Async;

namespace Maverick.Json.Benchmarks
{
    class Program
    {
        public static Random Rng { get; } = new Random();


        static async Task Main( String[] args )
        {
            //var objs = Enumerable.Range( 0, 100000 ).Select( x => LargeObject.Create() ).ToList();

            //using ( var buffer = new JsonAsyncStreamWriter( Stream.Null ) )
            //{
            //    await new JsonAsyncWriter( buffer ).WriteValueAsync( objs );
            //    await buffer.FlushAsync();
            //}
            BenchmarkSwitcher.FromAssembly( typeof( Program ).Assembly ).Run( args );
        }


        public static unsafe String RandomString( Int32 length, String alphabet )
        {
            var result = new String( ' ', length );

            fixed ( Char* resultFixed = result )
            {
                for ( var i = 0; i < result.Length; ++i )
                {
                    resultFixed[ i ] = alphabet[ Rng.Next( 0, alphabet.Length ) ];
                }
            }

            return result;
        }
    }
}
