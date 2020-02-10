using System;

namespace Maverick.Json
{
    /// <summary>
    /// Snapshot of the current state of a <seealso cref="JsonReader"/>.
    /// </summary>
    public readonly struct JsonReaderState
    {
        internal JsonReaderState( JsonReader reader,
                                  ReadOnlyMemory<Byte> memory,
                                  Int32 offset,
                                  JsonToken currentToken,
                                  JsonToken previousToken,
                                  Boolean expectComma,
                                  in JsonReaderStack stack )
        {
            Reader = reader;
            Position = reader.Position;
            Memory = memory;
            Offset = offset;
            CurrentToken = currentToken;
            PreviousToken = previousToken;
            ExpectComma = expectComma;
            Stack = stack;
        }


        public JsonReader Reader { get; }
        public SequencePosition Position { get; }
        internal ReadOnlyMemory<Byte> Memory { get; }
        internal Int32 Offset { get; }
        internal JsonToken CurrentToken { get; }
        internal JsonToken PreviousToken { get; }
        internal Boolean ExpectComma { get; }
        internal JsonReaderStack Stack { get; }
    }
}
