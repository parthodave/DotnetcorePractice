using DotNet8WebAPI.Messages;

namespace DotNet8WebAPI
{
    public interface IServiceBusService
    {
        Task SendBookCreatedAsync(BookCreatedMessage message);
    }
}