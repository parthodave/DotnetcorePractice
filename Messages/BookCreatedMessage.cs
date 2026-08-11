namespace DotNet8WebAPI.Messages
{
    public sealed class BookCreatedMessage
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();

        public int BookId { get; init; }

        public string BookName { get; init; } = string.Empty;

        public string BookAuthor { get; init; } = string.Empty;

        public DateTime CreatedOnUtc { get; init; } = DateTime.UtcNow;
    }
}