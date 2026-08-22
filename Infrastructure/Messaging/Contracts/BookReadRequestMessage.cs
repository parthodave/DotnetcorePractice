namespace DotNet8WebAPI.Infrastructure.Messaging.Contracts
{
    public enum BookReadAction
    {
        GetAll,
        GetById
    }

    public sealed class BookReadRequestMessage
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        public BookReadAction Action { get; init; }

        public int? Id { get; init; }
    }
}
