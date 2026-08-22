using System.Text.Json;
using Azure.Messaging.ServiceBus;
using DotNet8WebAPI.Infrastructure.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DotNet8WebAPI.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Background consumer that answers Book read requests (GetAll/GetById) by querying
    /// the database and replying on a session-enabled reply queue, tagged with the
    /// requester's correlation id so the caller receives only its own response.
    /// </summary>
    public sealed class BookReadRequestConsumerService : BackgroundService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookReadRequestConsumerService> _logger;
        private ServiceBusSender? _replySender;

        public BookReadRequestConsumerService(
            ServiceBusClient serviceBusClient,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<BookReadRequestConsumerService> logger)
        {
            _serviceBusClient = serviceBusClient;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var requestQueueName = _configuration["ServiceBus:ReadRequestQueueName"];
            var replyQueueName = _configuration["ServiceBus:ReadReplyQueueName"];

            if (string.IsNullOrWhiteSpace(requestQueueName))
            {
                throw new InvalidOperationException("Service Bus read request queue name ('ServiceBus:ReadRequestQueueName') is not configured.");
            }

            if (string.IsNullOrWhiteSpace(replyQueueName))
            {
                throw new InvalidOperationException("Service Bus read reply queue name ('ServiceBus:ReadReplyQueueName') is not configured.");
            }

            _replySender = _serviceBusClient.CreateSender(replyQueueName);

            await using var receiver = _serviceBusClient.CreateReceiver(requestQueueName);

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
                    _logger.LogError(ex, "Book read request consumer loop failed.");
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
                var request = JsonSerializer.Deserialize<BookReadRequestMessage>(message.Body.ToString(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request is null)
                {
                    _logger.LogWarning("Received an empty or invalid book read request. MessageId: {MessageId}", message.MessageId);
                    await receiver.DeadLetterMessageAsync(
                        message,
                        deadLetterReason: "InvalidPayload",
                        deadLetterErrorDescription: "BookReadRequestMessage payload could not be deserialized.",
                        cancellationToken: cancellationToken);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OurHeroDbContext>();

                var response = request.Action switch
                {
                    BookReadAction.GetAll => new BookReadResponseMessage
                    {
                        CorrelationId = request.CorrelationId,
                        Found = true,
                        Books = await dbContext.Books.ToListAsync(cancellationToken)
                    },
                    BookReadAction.GetById => await BuildGetByIdResponseAsync(dbContext, request, cancellationToken),
                    _ => new BookReadResponseMessage { CorrelationId = request.CorrelationId, Found = false }
                };

                await SendReplyAsync(message, request.CorrelationId, response, cancellationToken);
                await receiver.CompleteMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process book read request message. MessageId: {MessageId}", message.MessageId);
                await receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
            }
        }

        private static async Task<BookReadResponseMessage> BuildGetByIdResponseAsync(
            OurHeroDbContext dbContext,
            BookReadRequestMessage request,
            CancellationToken cancellationToken)
        {
            var book = request.Id.HasValue
                ? await dbContext.Books.FirstOrDefaultAsync(b => b.Id == request.Id.Value, cancellationToken)
                : null;

            return new BookReadResponseMessage
            {
                CorrelationId = request.CorrelationId,
                Found = book is not null,
                Books = book is not null ? new() { book } : new()
            };
        }

        private async Task SendReplyAsync(
            ServiceBusReceivedMessage originalMessage,
            Guid correlationId,
            BookReadResponseMessage response,
            CancellationToken cancellationToken)
        {
            var replyMessage = new ServiceBusMessage(JsonSerializer.Serialize(response))
            {
                ContentType = "application/json"
            };

            await _replySender!.SendMessageAsync(replyMessage, cancellationToken);

            _logger.LogInformation(
                "Replied to book read request. CorrelationId: {CorrelationId}, Found: {Found}",
                correlationId,
                response.Found);
        }
    }
}
