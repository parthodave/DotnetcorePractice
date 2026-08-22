using DotNet8WebAPI.Infrastructure.Messaging.Contracts;
using DotNet8WebAPI.Model;

namespace DotNet8WebAPI.Application.Books
{
    public interface IBookService
    {
        Task<List<Book>> GetAllBooks();

        Task<Book?> GetBookByID(int id);

        Task<BookCommandMessage> AddBook(Book obj);

        Task<BookCommandMessage?> UpdateBook(int id, Book obj);

        Task<bool> DeleteBookById(int id);
    }
}
