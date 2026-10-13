using Application.Abstractions;
using System.Threading.Channels;

namespace Infastructure.Messaging
{
    public class ChannelQueue<T> : IChannelQueue<T>
    {
        private readonly Channel<T> _channel;

        public ChannelQueue(int capacity = 1000) // arbitrary number for simulation purposes
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait  // backpressure control, blocks producer when the channel is full
            };

            _channel = Channel.CreateBounded<T>(options);
        }

        public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(item, cancellationToken);
        }

        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
