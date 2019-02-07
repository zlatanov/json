using System;

namespace Maverick.Json.Benchmarks
{
    public class LargeObject
    {
        public Int64 Id { get; set; }
        public Int32 Age { get; set; }
        public DateTime Time { get; set; }
        public DateTimeOffset TimeUtc { get; set; }
        public Decimal Amount { get; set; }
        public String Description { get; set; }
        public Guid Uid { get; set; }
        public Double Double { get; set; }
        public Single Single { get; set; }
        public Byte X { get; set; }
        public Byte Y { get; set; }
        public Byte Z { get; set; }
        public Decimal Probability { get; set; }
        public Decimal Price { get; set; }


        public static LargeObject Create()
        {
            var rng = Program.Rng;
            var obj = new LargeObject
            {
                Id = rng.Next( 0, Int32.MaxValue ),
                Age = rng.Next( 0, 70 ),
                Amount = (Decimal)( rng.NextDouble() * 1000 ),
                Description = Program.RandomString( rng.Next( 1, 30 ), "The quick brown fox jumps over the lazy dog" ),
                Double = rng.NextDouble(),
                Price = (Decimal)( rng.NextDouble() * 10 ),
                Probability = (Decimal)rng.NextDouble(),
                Single = rng.Next( 0, Int16.MaxValue ),
                Time = DateTime.Now,
                TimeUtc = DateTime.UtcNow,
                Uid = Guid.NewGuid(),
                X = (Byte)rng.Next( 0, 255 ),
                Y = (Byte)rng.Next( 0, 255 ),
                Z = (Byte)rng.Next( 0, 255 )
            };

            return obj;
        }



    }
}
