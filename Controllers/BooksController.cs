using DotNet8WebAPI.Helpers;
using DotNet8WebAPI.Model;
using DotNet8WebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotNet8WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize]
    public class BooksController : Controller
    {
        private readonly IBookService _bookService;
        public BooksController(IBookService bookService)
        {
            _bookService = bookService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBook()
        {
            var heros = await _bookService.GetAllBooks();
            return Ok(heros);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var hero = await _bookService.GetBookByID(id);
            if (hero == null)
            {
                return NotFound();
            }
            return Ok(hero);
        }

        [HttpPost]
        public async Task<IActionResult> AddBook([FromBody] Book heroObject)
        {
            var hero = await _bookService.AddBook(heroObject);

            if (hero == null)
            {
                return BadRequest();
            }

            return Ok(new
            {
                message = "Book Created Successfully!!!",
                id = hero!.Id
            });
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Book heroObject)
        {
            var hero = await _bookService.UpdateBook(id, heroObject);
            if (hero == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                message = "Book Updated Successfully!!!",
                id = hero!.Id
            });
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            if (!await _bookService.DeleteBookById(id))
            {
                return NotFound();
            }

            return Ok(new
            {
                message = "Book Deleted Successfully!!!",
                id = id
            });
        }
    }
}
