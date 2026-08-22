using System.Text.Json;
using Azure.Messaging.ServiceBus;
using DotNet8WebAPI.Infrastructure.Messaging.Contracts;
using DotNet8WebAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace DotNet8WebAPI.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Background consumer that applies queued Book write commands (Create/Update/Delete)
    /// to the database. This is the only place Book rows are mutated.
    /// </summary>
    public sealed class BookCommandConsumerService : BackgroundService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookCommandConsumerService> _logger;

        public BookCommandConsumerService(
            ServiceBusClient serviceBusClient,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<BookCommandConsumerService> logger)
        {
            _serviceBusClient = serviceBusClient;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = _configuration["ServiceBus:CommandQueueName"];

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new InvalidOperationException("Service Bus command queue name ('ServiceBus:CommandQueueName') is not configured.");
            }

            await using var receiver = _serviceBusClient.CreateReceiver(queueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var messages = await receiver.ReceiveMessagesAsync(
                        maxMessages: 10,
                        maxWaitTime: TimeSpan.FromSeconds(5),
                        cancellationToken: stoppingToken);

                    if (messages.Count == 0)
                    {
                        continue;
                    }

                    foreach (var message in messages)
                    {
                        await ProcessMessageAsync(receiver, message, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Book command consumer loop failed.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task ProcessMessageAsync(
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = JsonSerializer.Deserialize<BookCommandMessage>(message.Body.ToString(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (command is null)
                {
                    _logger.LogWarning("Received an empty or invalid book command. MessageId: {MessageId}", message.MessageId);
                    await receiver.DeadLetterMessageAsync(
                        message,
                        deadLetterReason: "InvalidPayload",
                        deadLetterErrorDescription: "BookCommandMessage payload could not be deserialized.",
                        cancellationToken: cancellationToken);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OurHeroDbContext>();

                switch (command.Action)
                {
                    case BookCommandAction.Create:
                        await HandleCreateAsync(dbContext, command, cancellationToken);
                        break;

                    case BookCommandAction.Update:
                        await HandleUpdateAsync(dbContext, command, cancellationToken);
                        break;

                    case BookCommandAction.Delete:
                        await HandleDeleteAsync(dbContext, command, cancellationToken);
                        break;

                    default:
                        _logger.LogWarning("Unknown book command action: {Action}", command.Action);
                        break;
                }

                await receiver.CompleteMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process book command message. MessageId: {MessageId}", message.MessageId);
                await receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
            }
        }

        private async Task HandleCreateAsync(OurHeroDbContext dbContext, BookCommandMessage command, CancellationToken cancellationToken)
        {
            var book = new Book
            {
                BookName = command.BookName,
                BookAuthor = command.BookAuthor
            };

            dbContext.Books.Add(book);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created book from command. MessageId: {MessageId}, BookName: {BookName}", command.MessageId, command.BookName);
        }

        private async Task HandleUpdateAsync(OurHeroDbContext dbContext, BookCommandMessage command, CancellationToken cancellationToken)
        {
            var book = await dbContext.Books.FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

            if (book is null)
            {
                _logger.LogWarning("Update command received for non-existent book. BookId: {BookId}", command.Id);
                return;
            }

            book.BookName = command.BookName;
            book.BookAuthor = command.BookAuthor;

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated book from command. MessageId: {MessageId}, BookId: {BookId}", command.MessageId, command.Id);
        }

        private async Task HandleDeleteAsync(OurHeroDbContext dbContext, BookCommandMessage command, CancellationToken cancellationToken)
        {
            var book = await dbContext.Books.FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

            if (book is null)
            {
                _logger.LogWarning("Delete command received for non-existent book. BookId: {BookId}", command.Id);
                return;
            }

            dbContext.Books.Remove(book);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted book from command. MessageId: {MessageId}, BookId: {BookId}", command.MessageId, command.Id);
        }
    }
}
