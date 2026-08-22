using DotNet8WebAPI.Infrastructure.Messaging.Contracts;

namespace DotNet8WebAPI.Infrastructure.Messaging
{
    public interface IServiceBusService
    {
        Task SendBookCommandAsync(BookCommandMessage message, CancellationToken cancellationToken = default);

        Task<BookReadResponseMessage> SendBookReadRequestAsync(BookReadRequestMessage request, CancellationToken cancellationToken = default);
    }
}