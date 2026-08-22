using DotNet8WebAPI.Model;

namespace DotNet8WebAPI.Infrastructure.Messaging.Contracts
{
    public sealed class BookReadResponseMessage
    {
        public Guid CorrelationId { get; init; }

        public bool Found { get; init; }

        public List<Book> Books { get; init; } = new();
    }
}
