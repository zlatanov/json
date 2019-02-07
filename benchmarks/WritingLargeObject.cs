﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Maverick.Json.Benchmarks
{
    [RyuJitX64Job, MemoryDiagnoser, CategoriesColumn]
    [GroupBenchmarksBy( BenchmarkLogicalGroupRule.ByCategory )]
    public class WritingLargeObject : Newtonsoft.Json.IArrayPool<Char>
    {
        public List<LargeObject> Objects { get; set; }


        [GlobalSetup]
        public void Setup()
        {
            Objects = Enumerable.Range( 0, 10000 ).Select( x => LargeObject.Create() ).ToList();
        }


        [Benchmark( Baseline = true )]
        [BenchmarkCategory( "Write" )]
        public void WriteNewtonsoft()
        {
            var serializer = Newtonsoft.Json.JsonSerializer.CreateDefault();
            var writer = new Newtonsoft.Json.JsonTextWriter( new StreamWriter( Stream.Null ) )
            {
                ArrayPool = this
            };
            serializer.Serialize( writer, Objects );
            writer.Close();
        }


        [Benchmark]
        [BenchmarkCategory( "Write" )]
        public void WriteMaverick()
        {
            using ( var buffer = new JsonStreamWriter( Stream.Null ) )
            {
                new JsonWriter( buffer ).WriteValue( Objects );
            }
        }


        Char[] Newtonsoft.Json.IArrayPool<Char>.Rent( Int32 minimumLength ) => ArrayPool<Char>.Shared.Rent( minimumLength );


        void Newtonsoft.Json.IArrayPool<Char>.Return( Char[] array ) => ArrayPool<Char>.Shared.Return( array );
    }
}
