using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using Maverick.Json.TestObjects;
using Xunit;
using NJ = Newtonsoft.Json;

namespace Maverick.Json
{
    /// <summary>
    /// The purpose of this tests is to test the correctness of the overall
    /// serialization / deserialization of objects compared to Newtonsoft.Json.
    /// </summary>
    public sealed class CorrectnessTests
    {
        public CorrectnessTests()
        {
            var settings = new NJ.JsonSerializerSettings
            {
                NullValueHandling = NJ.NullValueHandling.Ignore
            };

            NJ.JsonConvert.DefaultSettings = () => settings;
        }


        [Fact]
        public void Booleans() => Test( new
        {
            TestTrue = true,
            TestFalse = false,
            TestNullable = (Boolean?)true,
            TestNull = default( Boolean? ),
            TestObj = (Object)false,
        } );


        [Fact]
        public void Number_Byte() => Test( new
        {
            Test = (Byte)15,
            TestMin = Byte.MinValue,
            TextMax = Byte.MaxValue,
            TestNullable = (Byte?)77,
            TestNull = default( Byte? ),
            TestObj = (Object)(Byte)22,
            TestS = (SByte)12,
            TestSNegative = (SByte)( -25 ),
            TestSZero = (SByte)0,
            TestSMin = SByte.MinValue,
            TextSMax = SByte.MaxValue,
            TestSNullable = (SByte?)56,
            TestSNull = default( SByte? ),
            TestSObj = (Object)(SByte)56
        } );


        [Fact]
        public void Number_Int16() => Test( new
        {
            Test = (Int16)567,
            TestNegative = (Int16)( -2346 ),
            TestZero = (Int16)0,
            TestMin = Int16.MinValue,
            TestMax = Int16.MaxValue,
            TestNullable = (Int16?)7171,
            TestNull = default( Int16? ),
            TestObj = (Object)(Int16)18765,
            TestU = (UInt16)463,
            TestUMin = UInt16.MinValue,
            TestUMax = UInt16.MaxValue,
            TestUNullable = (UInt16?)35789,
            TestUNull = default( UInt16? ),
            TestUObj = (Object)(UInt16)22775
        } );


        [Fact]
        public void Number_Int32() => Test( new
        {
            Test = 456894,
            TestNegative = -5457457,
            TestZero = 0,
            TestMin = Int32.MinValue,
            TestMax = Int32.MaxValue,
            TestNullable = (Int32?)789756,
            TestNull = default( Int32? ),
            TestObj = (Object)256987,
            TestU = (UInt32)345345,
            TestUMin = UInt32.MinValue,
            TestUMax = UInt32.MaxValue,
            TestUNullable = (UInt32?)8937523,
            TestUNull = default( UInt32? ),
            TestUObj = (Object)(UInt32)2257943
        } );


        [Fact]
        public void Number_Int64() => Test( new
        {
            TestInt64 = 4562345894L,
            TestInt64Negative = -252352667343L,
            TestInt64Zero = 0L,
            TestInt64Min = Int64.MinValue,
            TestInt64Max = Int64.MaxValue,
            TestInt64Nullable = (Int64?)43589023423L,
            TestInt64Null = default( Int64? ),
            TestInt64Obj = (Object)3895163L,
            TestUInt64 = (UInt64)345345,
            TestUInt64Min = UInt64.MinValue,
            TestUInt64Max = UInt64.MaxValue,
            TestUInt64Nullable = (UInt64?)1000000000L,
            TestUInt64Null = default( UInt64? ),
            TestUInt64Obj = (Object)(UInt64)9626926352L
        } );


        [Fact]
        public void Numbers_Decimal() => Test( new
        {
            Test = 64235346.73214346M,
            TestNegative = -346893406.1324M,
            TestNullable = (Decimal?)879000.235M,
            TestNull = default( Decimal? ),
            TestObj = (Object)43582.3578145663M
        } );


        [Fact]
        public void Numbers_Decimal__Zero_Min_Max()
        {
            // Our serialization of whole number of decimal is better so we check it here
            var expected = $"{{\"TestZero\":{Decimal.Zero},\"TestMin\":{Decimal.MinValue},\"TestMax\":{Decimal.MaxValue}}}";
            var actual = JsonConvert.Serialize( new
            {
                TestZero = Decimal.Zero,
                TestMin = Decimal.MinValue,
                TestMax = Decimal.MaxValue
            } );

            Assert.Equal( expected, actual );
        }


        [Fact]
        public void Numbers_Single() => Test( new
        {
            Test = 2.5F,
            TestNegative = -5.234F,
            TestManyZeroes = 77800000000F,
            TestManyZeroesNegative = -15340000000F,
            TestLessThenOne = 0.00734623F,
            TestLessThenOneNegative = -0.23345F,
            TestLessThenOneManyZeros = 0.000000000025F,
            TestLessThenOneManyZerosNegative = 0.0000000000789F,
            TestMin = -3.402823E+38f, // We don't use the actual value becouse it's broken and adds extra symbols
            TestMax = 3.402823E+38f, // We don't use the actual values becouse it's broken and adds extra symbols
            TestNullable = (Single?)908754.1234F,
            TestNull = default( Single? ),
            TestObj = (Object)87.45F,
            TestNan = Single.NaN,
            TestNanNullable = (Single?)Single.NaN,
            TestNi = Single.NegativeInfinity,
            TestNiNullable = (Single?)Single.NegativeInfinity,
            TestPi = Single.PositiveInfinity,
            TestPiNullable = (Single?)Single.PositiveInfinity
        } );


        [Fact]
        public void Numbers_Double() => Test( new
        {
            Test = 2342.5D,
            TestNegative = -12.2435D,
            TestManyZeroes = 1534000000000000D,
            TestManyZeroesNegative = -2240000000000000D,
            TestLessThenOne = 0.0045D,
            TestLessThenOneNegative = -0.00345D,
            TestLessThenOneManyZeros = 0.000000000781D,
            TestLessThenOneManyZerosNegative = -0.000000000234D,
            TestMin = Double.MinValue,
            TestMax = Double.MaxValue,
            TestNullable = (Double?)34578.234D,
            TestNull = default( Double? ),
            TestObj = (Object)125.225D,
            TestNan = Double.NaN,
            TestNanNullable = (Double?)Double.NaN,
            TestNi = Double.NegativeInfinity,
            TestNiNullable = (Double?)Double.NegativeInfinity,
            TestPi = Double.PositiveInfinity,
            TestPiNullable = (Double?)Double.PositiveInfinity
        } );


        [Fact]
        public void TimeSpans() => Test( new
        {
            Test = new TimeSpan( 5, 6, 0, 50, 248 ),
            TestNegative = new TimeSpan( 30, 12, 18, 37, 865 ).Negate(),
            TestMs = TimeSpan.FromMilliseconds( 500 ),
            TestMsNegative = TimeSpan.FromMilliseconds( -250 ),
            TestSec = TimeSpan.FromSeconds( 12 ),
            TestSecNegative = TimeSpan.FromSeconds( -38 ),
            TestMinute = TimeSpan.FromMinutes( 27 ),
            TestMinuteNegative = TimeSpan.FromSeconds( -45 ),
            TestHour = TimeSpan.FromHours( 6 ),
            TestHourNegative = TimeSpan.FromSeconds( -12 ),
            TestDays = TimeSpan.FromHours( 756 ),
            TestDaysNegative = TimeSpan.FromSeconds( -82 ),
            TestMin = TimeSpan.MinValue,
            TestMax = TimeSpan.MaxValue,
            TestZero = TimeSpan.Zero,
            TestNullable = (TimeSpan?)new TimeSpan( 5, 0, 12, 0, 765 ),
            TestNull = default( TimeSpan? ),
            TestObj = (Object)new TimeSpan( 2, 6, 32, 0, 0 )
        } );


        [Fact]
        public void OneDateTime() => Test( new DateTime( 2015, 5, 23, 6, 43, 50 ) );


        [Fact]
        public void DateTimes() => Test( new
        {
            Test = new DateTime( 2015, 5, 23, 6, 43, 50 ),
            TestNow = DateTime.Now,
            TestUtcNow = DateTime.UtcNow,
            TestMin = DateTime.MinValue,
            TestMax = DateTime.MaxValue,
            TestNullable = (DateTime?)new DateTime( 2017, 3, 1, 22, 15, 27 ),
            TestNull = default( DateTime? ),
            TestObj = (Object)new DateTime( 2032, 5, 24, 12, 00, 00 ),
            TestNoTrailingZeroTicks = DateTime.Parse( "2018-06-26T13:20:07.8990000+03:00" ),
            TestLeadingZeroTicks = DateTime.Parse( "2018-06-26T13:28:12.0006752+03:00" )
        } );


        [Fact]
        public void DateTimeOffsets() => Test( new
        {
            Test = new DateTimeOffset( 2020, 7, 15, 11, 25, 18, TimeSpan.FromMinutes( 30 ) ),
            TestNegative = new DateTimeOffset( 2016, 3, 5, 15, 57, 30, TimeSpan.FromMinutes( -90 ) ),
            TestMin = DateTimeOffset.MinValue,
            TestMax = DateTimeOffset.MaxValue,
            TestNullable = (DateTimeOffset?)new DateTimeOffset( 2000, 1, 1, 0, 0, 0, TimeSpan.FromHours( 14 ) ),
            TestNull = default( DateTimeOffset? ),
            TestObj = (Object)new DateTimeOffset( 2050, 12, 25, 23, 30, 30, TimeSpan.FromMinutes( 215 ) ),
        } );


        [Fact]
        public void OneChar() => Test( 'x' );


        [Fact]
        public void OneString() => Test( "Ivan Zlatanov" );


        [Fact]
        public void OneInt64() => Test( Int64.MaxValue );


        [Fact]
        public void OneUInt64() => Test( UInt64.MaxValue );


        [Fact]
        public void Chars() => Test( new
        {
            Test = 'a',
            TestMin = Char.MinValue,
            TestMax = Char.MaxValue,
            TestNullable = (Char?)'b',
            TestNull = default( Char? ),
            TestObj = (Object)'c'
        } );


        [Fact]
        public void Strings() => Test( new
        {
            Test = "XZ9000 is a random robot name.",
            TestEscape = "\",\\,  ,",
            TestMultiline = @"line 1
line 2",
            TestEmpty = String.Empty,
            TestNull = default( String ),
            TestObj = (Object)"string as object",
            Emoji = "🍄🌶🍓\u2028",
            Bulgarian = "Български",
            ReallyLongEmoji = String.Join( ", ", Enumerable.Repeat( 0, 10000 ).Select( x => "🍄🌶🍓\u2028" ) )
        } );


        [Fact]
        public void Strings_Escape_Html()
        {
            var expected = "{\"TestNoEscape\":\"/\",\"TestEscapeHtml\":\"<\\/script>\"}";
            var actual = JsonConvert.Serialize( new
            {
                TestNoEscape = "/",
                TestEscapeHtml = "</script>"
            } );

            Assert.Equal( expected, actual );
        }


        [Fact]
        public void Guids() => Test( new
        {
            Test = Guid.NewGuid(),
            TestEmpty = Guid.Empty,
            TestNullable = (Guid?)Guid.NewGuid(),
            TestNull = default( Guid? ),
            TestObj = (Object)Guid.NewGuid()
        } );


        [Fact]
        public void ByteArray() => Test( new
        {
            Test = GetByteArray(),
            TestNull = default( Byte[] ),
            TestObj = (Object)GetByteArray(),
        } );


        private Byte[] GetByteArray()
        {
            var result = new Byte[ Byte.MaxValue + 1 ];

            for ( var i = Byte.MinValue; i < Byte.MaxValue; )
            {
                result[ i + 1 ] = ++i;
            }

            return result;
        }


        private enum EnumByte : Byte { One = 1, Two = 2 }


        [Fact]
        public void Enums_Byte() => Test( new
        {
            Test = EnumByte.One,
            TestOutOfBounds = (EnumByte)30,
            TestNullable = (EnumByte?)EnumByte.One,
            TestNull = default( EnumByte? ),
            TestObj = (Object)EnumByte.One
        } );


        private enum EnumInt16 : Int16 { Int16One = 1 }
        private enum EnumUInt16 : UInt16 { UInt16One = 1 }


        [Fact]
        public void Enums_Int16() => Test( new
        {
            Test = EnumInt16.Int16One,
            TestOutOfBounds = (EnumInt16)30,
            TestNullable = (EnumInt16?)EnumInt16.Int16One,
            TestNull = default( EnumInt16? ),
            TestObj = (Object)EnumInt16.Int16One,
            TestU = EnumUInt16.UInt16One,
            TestUOutOfBounds = (EnumUInt16)30,
            TestUNullable = (EnumUInt16?)EnumUInt16.UInt16One,
            TestUNull = default( EnumUInt16? ),
            TestUObj = (Object)EnumUInt16.UInt16One
        } );


        private enum EnumInt32 : Int32 { Int32One = 1 }
        private enum EnumUInt32 : UInt32 { UInt32One = 1 }


        [Fact]
        public void Enums_Int32() => Test( new
        {
            Test = EnumInt32.Int32One,
            TestOutOfBounds = (EnumInt32)30,
            TestNullable = (EnumInt32?)EnumInt32.Int32One,
            TestNull = default( EnumInt32? ),
            TestObj = (Object)EnumInt32.Int32One,
            TestU = EnumUInt32.UInt32One,
            TestUOutOfBounds = (EnumUInt32)30,
            TestUNullable = (EnumUInt32?)EnumUInt32.UInt32One,
            TestUNull = default( EnumUInt32? ),
            TestUObj = (Object)EnumUInt32.UInt32One
        } );


        private enum EnumInt64 : Int64 { Int64One = 1 }
        private enum EnumUInt64 : UInt64 { UInt64One = 1 }


        [Fact]
        public void Enums_Int64() => Test( new
        {
            Test = EnumInt64.Int64One,
            TestOutOfBounds = (EnumInt64)30,
            TestNullable = (EnumInt64?)EnumInt64.Int64One,
            TestNull = default( EnumInt64? ),
            TestObj = (Object)EnumInt64.Int64One,
            TestU = EnumUInt64.UInt64One,
            TestUOutOfBounds = (EnumUInt64)30,
            TestUNullable = (EnumUInt64?)EnumUInt64.UInt64One,
            TestUNull = default( EnumUInt64? ),
            TestUObj = (Object)EnumUInt64.UInt64One
        } );


        [Fact]
        public void Objects()
        {
            dynamic dynamicObject = new ExpandoObject();
            dynamicObject.Name = "John Doe";
            dynamicObject.Age = 30;
            dynamicObject.Cash = 100.12M;

            Test( new
            {
                Test = new Object(),
                TestNull = default( Object ),
                TestDynamic = dynamicObject,
                TestDynamicNull = default( dynamic ),
                TestDynamicObj = (Object)dynamicObject,
                Version = new Version( 1, 2, 3, 4 )
            } );
        }


        [Fact]
        public void Arrays() => Test( new
        {
            TestNumber = new[] { 20, 50, 70, 200 },
            TestNumberEmpty = new Int32[ 0 ],
            TestNumberNull = default( Int32[] ),
            TestNumberObj = (Object)new[] { 2, 5, 7, 3645, 425, 123, 5 },
            TestString = new[] { "string 1", "string 2", "string 3" },
            TestStringEmpty = new String[ 0 ],
            TestStringNull = default( DateTime[] ),
            TestStringObj = (Object)new[] { "ala bala", "Ali Baba", "an object" },
            TestDateTime = new[] { new DateTime( 2018, 7, 25, 22, 35, 6 ), DateTime.Now, DateTime.UtcNow },
            TestDateTimeEmpty = new DateTime[ 0 ] { },
            TestDateTimeNull = default( DateTime[] ),
            TestDateTimeObj = (Object)new[] { DateTime.Now, DateTime.UtcNow },
            TestMixed = new Object[] { 50, 2.5D, "string 7", DateTime.Now },
            TestMixedEmpty = new Object[ 0 ],
            TestMixedNull = default( Object[] ),
            TestMixedObj = (Object)new Object[] { 7, "fake number string follows", "566", DateTime.Now },
        } );


        [Fact]
        public void Arrays_MultiLevel()
        {
            var doubleArray = new Int32[ 2, 2 ];
            doubleArray[ 0, 0 ] = 1;
            doubleArray[ 0, 1 ] = 2;
            doubleArray[ 1, 0 ] = 3;
            doubleArray[ 1, 1 ] = 4;

            var trippleArray = new Int32[ 2, 2, 2 ];
            trippleArray[ 0, 0, 0 ] = 1;
            trippleArray[ 0, 0, 1 ] = 2;
            trippleArray[ 0, 1, 0 ] = 3;
            trippleArray[ 0, 1, 1 ] = 4;
            trippleArray[ 1, 0, 0 ] = 5;
            trippleArray[ 1, 0, 1 ] = 6;
            trippleArray[ 1, 1, 0 ] = 7;
            trippleArray[ 1, 1, 1 ] = 8;

            Test( new
            {
                TestDouble = doubleArray,
                TestDoubleEmpty = new Int32[ 0, 0 ],
                TestDoubleNull = default( Int32[,] ),
                TestDoubleObj = (Object)doubleArray,
                TestTripple = trippleArray,
                TestTrippleEmpty = new Int32[ 0, 0, 0 ],
                TestTrippleNull = default( Int32[,,] ),
                TestTrippleObj = (Object)trippleArray
            } );
        }


        [Fact]
        public void Collections() => Test( new
        {
            TestEnumerable = Enumerable.Range( 0, 10 ),
            TestEnumerableEmpty = Enumerable.Empty<Int32>(),
            TestEnumerableNull = default( IEnumerable<Int32> ),
            TestEnumerableObj = (Object)Enumerable.Range( 0, 10 ),
            TestList = new List<Int32>() { 1, 2, 3, 4, 5, 6, 7, 8 },
            TestListEmpty = new List<Int32>(),
            TestListNull = default( List<Int32> ),
            TestListObj = (Object)new List<Int32>() { 10, 20, 30, 40, 50, 60, 70, 80 },
            TestArrayList = new ArrayList() { 1, 2, "Barry" },
            TestArrayListEmpty = new ArrayList(),
            TestArrayListNull = default( ArrayList ),
            TestArrayListObj = (Object)new ArrayList() { 1, 2, "Barry" },
            TestHashset = new HashSet<Int32>() { 15, 30, 35, 25, 1234, 6, 3245, 341, 4, 2436, 31, 4 },
            TestHashsetEmpty = new HashSet<Int32>(),
            TestHashsetNull = default( HashSet<Int32> ),
            TestHashsetObj = new HashSet<Int32>() { 235, 123, 25, 123, 24, 1324, 346, 324, 246, 2, 4124, 234 },
            TestDictionary = new Dictionary<EnumByte, String>() { [ EnumByte.One ] = "String One", [ EnumByte.Two ] = "String Two" },
            TestDictionaryEmpty = new Dictionary<EnumUInt32, String>(),
            TestDictionaryNull = default( Dictionary<EnumUInt32, String> ),
            TestDictionaryObj = (Object)new Dictionary<String, String>() { [ "Key One" ] = "String One", [ "Key Two" ] = "String Two" },
            TestConcurrentDictionary = new ConcurrentDictionary<EnumByte, Object>() { [ EnumByte.Two ] = "Two" },
        } );


        [Fact]
        public void ImmutableCollections() => Test( new
        {
            Array = ImmutableArray.CreateRange( Enumerable.Range( 1, 10 ) ),
            List = ImmutableList.Create( new[] { "String 1", "String 2", "String 3" } ),
        } );


        [Fact]
        public void MultiLevel() => Test( new
        {
            testMultiLevelSub = new
            {
                testMultiLevel1Sub = new
                {
                    testMultiLevel2String = "Level 2",
                    testMultiLevel2Number = 2
                },
                testMultiLevel1Sub2 = new
                {
                    testMultiLevel2Sub = new
                    {
                        testMultiLevel3Sub = new
                        {
                            testMultiLevel4Sub = new
                            {
                                testMultiLevel5Sub = new
                                {
                                    testMultiLevel6Sub = new
                                    {
                                        testMultiLevel7Sub = new
                                        {
                                            testMultiLevel8Sub = new
                                            {
                                                testMultiLevel9Sub = new
                                                {
                                                    testMultiLevel10Sub = new
                                                    {
                                                        testMultiLevel11Sub = new
                                                        {
                                                            testMultiLevel12Sub = new
                                                            {
                                                                testMultiLevel13Sub = new
                                                                {
                                                                    testMultiLevel14Sub = new
                                                                    {
                                                                        testMultiLevel15String = "Level 15",
                                                                    },
                                                                    testMultiLevel14String = "Level 14",
                                                                },
                                                                testMultiLevel13String = "Level 13",
                                                            },
                                                            testMultiLevel12String = "Level 12",
                                                        },
                                                        testMultiLevel11String = "Level 11",
                                                    },
                                                    testMultiLevel10String = "Level 10",
                                                },
                                                testMultiLevel9String = "Level 9",
                                            },
                                            testMultiLevel8String = "Level 8",
                                        },
                                        testMultiLevel7String = "Level 7",
                                    },
                                    testMultiLevel6String = "Level 6",
                                },
                                testMultiLevel5String = "Level 5",
                            },
                            testMultiLevel4String = "Level 4",
                        },
                        testMultiLevel3String = "Level 3",
                    },
                    testMultiLevel2String = "Level 2",
                    testMultiLevel2Number = 2.5D
                },
                testMultiLevel1String = "Level 1",
                testMultiLevel1DateTime = DateTime.Now
            },
            testMultiLevelSub2 = new
            {
                testMultiLevel1String = "Level 1",
                testMultiLevel1DateTime = DateTime.UtcNow
            },
            testMultiLevelString = "Level 0"
        } );


        [Fact]
        public void Dynamic()
        {
            dynamic value = new ExpandoObject();

            value.Name = "John Doe";
            value.Age = 30;
            value.Cash = 100.12M;

            Test( new
            {
                Value = value
            } );
        }


        [Fact]
        public void Dog() => Test( new Dog
        {
            Breed = "Labrador",
            Name = "Barky"
        } );


        private static void Test<T>( T obj )
        {
            var expected = NJ.JsonConvert.SerializeObject( obj );
            var actual = JsonConvert.Serialize( obj );

            Assert.Equal( expected, actual );

            // Test deserialization
            var deserialziedObj = JsonConvert.Deserialize<T>( actual );
            var deserializedObjJson = JsonConvert.Serialize( deserialziedObj );

            Assert.Equal( actual, deserializedObjJson );

            // Test formatting
            using ( var stream = new MemoryStream() )
            {
                using ( var sw = new StreamWriter( stream ) )
                using ( var writer = new NJ.JsonTextWriter( sw ) )
                {
                    writer.Formatting = NJ.Formatting.Indented;
                    writer.Indentation = 4;
                    writer.IndentChar = ' ';

                    var serializer = NJ.JsonSerializer.CreateDefault();

                    serializer.Serialize( writer, obj );
                }

                expected = Encoding.UTF8.GetString( stream.ToArray() );
            }
            actual = JsonConvert.Serialize( obj, JsonFormat.Indented );

            Assert.Equal( expected, actual );
        }
    }
}
