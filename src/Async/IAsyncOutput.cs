using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Maverick.Json.Async
{
    public interface IAsyncOutput : IBufferWriter<Byte>
    {
        Task FlushAsync( CancellationToken cancellationToken );
    }
}
