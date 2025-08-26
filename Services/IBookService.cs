using DotNet8WebAPI.Model;

namespace DotNet8WebAPI.Services
{
    public interface IBookService
    {
        Task<List<Book>> GetAllBooks();

        Task<Book?> GetBookByID(int id);

        Task<Book?> AddBook(Book obj);

        Task<Book?> UpdateBook(int id, Book obj);

        Task<bool> DeleteBookById(int id);
    }
}
