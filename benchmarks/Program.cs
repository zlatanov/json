using System;
using BenchmarkDotNet.Running;

namespace Maverick.Json.Benchmarks
{
    class Program
    {
        public static Random Rng { get; } = new Random();


        static void Main( String[] args ) => BenchmarkSwitcher.FromAssembly( typeof( Program ).Assembly ).Run( args );


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
