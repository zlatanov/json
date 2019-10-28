using System;

namespace Maverick.Json
{
    /// <summary>
    /// Snapshot of the current state of a <seealso cref="JsonReader"/>.
    /// </summary>
    public sealed class JsonReaderState
    {
        internal JsonReaderState()
        {
        }


        public JsonReader Reader { get; internal set; }
        public SequencePosition Position { get; internal set; }
        internal ReadOnlyMemory<Byte> Memory { get; set; }
        internal Int32 Offset { get; set; }
        internal  JsonToken CurrentToken { get; set; }
        internal Boolean ExpectComma { get; set; }
        internal Int32 LineNumber { get; set; }
        internal JsonToken[] Tokens { get; set; }
    }
}
