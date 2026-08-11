using DotNet8WebAPI.Model;
using Microsoft.EntityFrameworkCore;
using DotNet8WebAPI.Messages;

namespace DotNet8WebAPI.Services
{
    public class BookService : IBookService
    {
        private readonly OurHeroDbContext _db;
        private readonly IServiceBusService _serviceBusService;

        public BookService(
            OurHeroDbContext db,
            IServiceBusService serviceBusService)
        {
            _db = db;
            _serviceBusService = serviceBusService;
        }

        public async Task<List<Book>> GetAllBooks()
        {
             return await _db.Books.ToListAsync();

        }

        public async Task<Book?> GetBookByID(int id)
        {
            return await _db.Books.FirstOrDefaultAsync(hero => hero.Id == id);
        }

        public async Task<Book?> AddBook(Book obj)
        {
            var addHero = new Book()
            {
                BookName = obj.BookName,
                BookAuthor = obj.BookAuthor,
            };

            _db.Books.Add(addHero);
            var result = await _db.SaveChangesAsync();
            return result >= 0 ? addHero : null;
        }

        public async Task<Book?> UpdateBook(int id, Book obj)
        {
            var hero = await _db.Books.FirstOrDefaultAsync(index => index.Id == id);
            if (hero != null)
            {
                hero.BookAuthor = obj.BookAuthor;
                hero.BookName = obj.BookName;

                var result = await _db.SaveChangesAsync();
                return result >= 0 ? hero : null;
            }
            return null;
        }

        public async Task<bool> DeleteBookById(int id)
        {
            var hero = await _db.Books.FirstOrDefaultAsync(index => index.Id == id);
            if (hero != null)
            {
                _db.Books.Remove(hero);
                var result = await _db.SaveChangesAsync();
                return result >= 0;
            }
            return false;
        }

    }
}
