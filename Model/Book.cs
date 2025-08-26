namespace DotNet8WebAPI.Model
{
    public class Book
    {
        public int Id { get; set; }
        public required string BookName { get; set; }
        public string BookAuthor { get; set; } = string.Empty;
    }
}
