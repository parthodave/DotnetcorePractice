using DotNet8WebAPI.Infrastructure.Messaging;
using DotNet8WebAPI.Infrastructure.Messaging.Contracts;
using DotNet8WebAPI.Model;

namespace DotNet8WebAPI.Application.Books
{
    public class BookService : IBookService
    {
        private readonly IServiceBusService _serviceBusService;

        public BookService(IServiceBusService serviceBusService)
        {
            _serviceBusService = serviceBusService;
        }

        public async Task<List<Book>> GetAllBooks()
        {
            var request = new BookReadRequestMessage { Action = BookReadAction.GetAll };
            var response = await _serviceBusService.SendBookReadRequestAsync(request);
            return response.Books;
        }

        public async Task<Book?> GetBookByID(int id)
        {
            var request = new BookReadRequestMessage { Action = BookReadAction.GetById, Id = id };
            var response = await _serviceBusService.SendBookReadRequestAsync(request);
            return response.Found ? response.Books.FirstOrDefault() : null;
        }

        public async Task<BookCommandMessage> AddBook(Book obj)
        {
            var command = new BookCommandMessage
            {
                Action = BookCommandAction.Create,
                BookName = obj.BookName,
                BookAuthor = obj.BookAuthor
            };

            await _serviceBusService.SendBookCommandAsync(command);
            return command;
        }

        public async Task<BookCommandMessage?> UpdateBook(int id, Book obj)
        {
            var existing = await GetBookByID(id);
            if (existing is null)
            {
                return null;
            }

            var command = new BookCommandMessage
            {
                Action = BookCommandAction.Update,
                Id = id,
                BookName = obj.BookName,
                BookAuthor = obj.BookAuthor
            };

            await _serviceBusService.SendBookCommandAsync(command);
            return command;
        }

        public async Task<bool> DeleteBookById(int id)
        {
            var existing = await GetBookByID(id);
            if (existing is null)
            {
                return false;
            }

            var command = new BookCommandMessage
            {
                Action = BookCommandAction.Delete,
                Id = id
            };

            await _serviceBusService.SendBookCommandAsync(command);
            return true;
        }
    }
}
