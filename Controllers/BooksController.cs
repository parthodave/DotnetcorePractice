using DotNet8WebAPI.Helpers;
using DotNet8WebAPI.Application.Books;
using DotNet8WebAPI.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights;

namespace DotNet8WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize]
    public class BooksController : Controller
    {
        private readonly IBookService _bookService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<BooksController> _logger;

        public BooksController(IBookService bookService, TelemetryClient telemetryClient, ILogger<BooksController> logger)
        {
            _bookService = bookService;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBook()
        {
            _logger.LogInformation("BooksController: GetAllBooks operation initiated");
            _telemetryClient.TrackEvent("Books.GetAll.Started");

            try
            {
                var heros = await _bookService.GetAllBooks();

                _telemetryClient.TrackEvent("Books.GetAll.Success", new Dictionary<string, string>
                {
                    { "BookCount", heros?.Count().ToString() ?? "0" }
                });

                _logger.LogInformation("Successfully retrieved {Count} books", heros?.Count() ?? 0);
                return Ok(heros);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackEvent("Books.GetAll.Failed", new Dictionary<string, string>
                {
                    { "Error", ex.Message }
                });
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation("BooksController: Get book by ID {BookId}", id);
            _telemetryClient.TrackEvent("Books.GetById.Started", new Dictionary<string, string>
            {
                { "BookId", id.ToString() }
            });

            var hero = await _bookService.GetBookByID(id);
            if (hero == null)
            {
                _telemetryClient.TrackEvent("Books.GetById.NotFound", new Dictionary<string, string>
                {
                    { "BookId", id.ToString() }
                });
                _logger.LogWarning("Book with ID {BookId} not found", id);
                return NotFound();
            }

            _telemetryClient.TrackEvent("Books.GetById.Success", new Dictionary<string, string>
            {
                { "BookId", id.ToString() },
                { "BookTitle", hero.BookName ?? "N/A" }
            });

            return Ok(hero);
        }

        [HttpPost]
        public async Task<IActionResult> AddBook([FromBody] Book heroObject)
        {
            _logger.LogInformation("BooksController: AddBook operation initiated for '{BookName}'", heroObject?.BookName);
            _telemetryClient.TrackEvent("Books.Add.Started", new Dictionary<string, string>
            {
                { "BookTitle", heroObject?.BookName ?? "N/A" }
            });

            var hero = await _bookService.AddBook(heroObject);

            _telemetryClient.TrackEvent("Books.Add.Queued", new Dictionary<string, string>
            {
                { "MessageId", hero.MessageId.ToString() },
                { "BookTitle", hero.BookName ?? "N/A" }
            });
            _logger.LogInformation("Queued book creation for '{BookName}' with message {MessageId}", hero.BookName, hero.MessageId);

            return Accepted(new
            {
                message = "Book creation queued successfully. It will be persisted by the Service Bus consumer.",
                messageId = hero.MessageId,
                hero.BookName,
                hero.BookAuthor,
                hero.CreatedOnUtc
            });
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Book heroObject)
        {
            _logger.LogInformation("BooksController: UpdateBook operation initiated for ID {BookId}", id);
            _telemetryClient.TrackEvent("Books.Update.Started", new Dictionary<string, string>
            {
                { "BookId", id.ToString() },
                { "BookTitle", heroObject?.BookName ?? "N/A" }
            });

            var hero = await _bookService.UpdateBook(id, heroObject);
            if (hero == null)
            {
                _telemetryClient.TrackEvent("Books.Update.NotFound", new Dictionary<string, string>
                {
                    { "BookId", id.ToString() }
                });
                _logger.LogWarning("Book with ID {BookId} not found for update", id);
                return NotFound();
            }

            _telemetryClient.TrackEvent("Books.Update.Success", new Dictionary<string, string>
            {
                { "BookId", hero!.Id.ToString() },
                { "BookTitle", hero.BookName ?? "N/A" }
            });
            _logger.LogInformation("Successfully queued update for book with ID {BookId}", hero.Id);

            return Accepted(new
            {
                message = "Book update queued successfully. It will be persisted by the Service Bus consumer.",
                messageId = hero.MessageId,
                id = hero.Id
            });
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            _logger.LogInformation("BooksController: DeleteBook operation initiated for ID {BookId}", id);
            _telemetryClient.TrackEvent("Books.Delete.Started", new Dictionary<string, string>
            {
                { "BookId", id.ToString() }
            });

            if (!await _bookService.DeleteBookById(id))
            {
                _telemetryClient.TrackEvent("Books.Delete.NotFound", new Dictionary<string, string>
                {
                    { "BookId", id.ToString() }
                });
                _logger.LogWarning("Book with ID {BookId} not found for deletion", id);
                return NotFound();
            }

            _telemetryClient.TrackEvent("Books.Delete.Success", new Dictionary<string, string>
            {
                { "BookId", id.ToString() }
            });
            _logger.LogInformation("Successfully queued deletion for book with ID {BookId}", id);

            return Accepted(new
            {
                message = "Book deletion queued successfully. It will be persisted by the Service Bus consumer.",
                id = id
            });
        }
    }
}
