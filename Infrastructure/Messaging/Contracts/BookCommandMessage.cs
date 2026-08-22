namespace DotNet8WebAPI.Infrastructure.Messaging.Contracts
{
    public enum BookCommandAction
    {
        Create,
        Update,
        Delete
    }

    public sealed class BookCommandMessage
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();

        public BookCommandAction Action { get; init; }

        public int Id { get; init; }

        public string BookName { get; init; } = string.Empty;

        public string BookAuthor { get; init; } = string.Empty;

        public DateTime CreatedOnUtc { get; init; } = DateTime.UtcNow;
    }
}
