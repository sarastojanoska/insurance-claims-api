
namespace Application.Abstractions
{
    public interface IChannelQueue<T>
    {
        ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
    }
}
