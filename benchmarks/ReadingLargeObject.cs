using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Maverick.Json.Benchmarks
{
    [RyuJitX64Job, MemoryDiagnoser, CategoriesColumn]
    [GroupBenchmarksBy( BenchmarkLogicalGroupRule.ByCategory )]
    public class ReadingLargeObject : Newtonsoft.Json.IArrayPool<Char>
    {
        public List<LargeObject> Objects { get; set; }
        public String ObjectsJson { get; set; }
        public Byte[] ObjectsJsonBytes { get; set; }


        [GlobalSetup]
        public void Setup()
        {
            Objects = Enumerable.Range( 0, 100000 ).Select( x => LargeObject.Create() ).ToList();
            ObjectsJson = JsonConvert.Serialize( Objects );
            ObjectsJsonBytes = Encoding.UTF8.GetBytes( ObjectsJson );
        }


        [Benchmark( Baseline = true )]
        [BenchmarkCategory( "Read" )]
        public void ReadNewtonsoft() => Newtonsoft.Json.JsonConvert.DeserializeObject<List<LargeObject>>( ObjectsJson );


        [Benchmark]
        [BenchmarkCategory( "Read" )]
        public void ReadMaverick()
        {
            JsonConvert.Deserialize<List<LargeObject>>( ObjectsJsonBytes );
        }


        [Benchmark]
        [BenchmarkCategory( "Read" )]
        public void ReadMicrosoft()
        {
            System.Text.Json.JsonSerializer.Deserialize<List<LargeObject>>( ObjectsJsonBytes );
        }



        Char[] Newtonsoft.Json.IArrayPool<Char>.Rent( Int32 minimumLength ) => ArrayPool<Char>.Shared.Rent( minimumLength );


        void Newtonsoft.Json.IArrayPool<Char>.Return( Char[] array ) => ArrayPool<Char>.Shared.Return( array );
    }
}
