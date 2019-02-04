using System;

namespace Maverick.Json.TestObjects
{
    public class Dog : Animal
    {
        public override String Name { get; set; }


        public String Breed { get; set; }


        public String Owner { get; set; }
        public Boolean ShouldSerializeOwner() => Owner?.Length > 0;
    }
}
