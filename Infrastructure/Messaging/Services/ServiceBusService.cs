using Azure.Messaging.ServiceBus;
using DotNet8WebAPI.Infrastructure.Messaging.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DotNet8WebAPI.Infrastructure.Messaging
{
    public class ServiceBusService : IServiceBusService
    {
        private static readonly TimeSpan ReadReplyTimeout = TimeSpan.FromSeconds(30);

        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusSender _commandSender;
        private readonly ServiceBusSender _readRequestSender;
        private readonly string _readReplyQueueName;
        private readonly ILogger<ServiceBusService> _logger;

        public ServiceBusService(
            ServiceBusClient serviceBusClient,
            IConfiguration configuration,
            ILogger<ServiceBusService> logger)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;

            var commandQueueName = configuration["ServiceBus:CommandQueueName"];
            var readRequestQueueName = configuration["ServiceBus:ReadRequestQueueName"];
            var readReplyQueueName = configuration["ServiceBus:ReadReplyQueueName"];

            if (string.IsNullOrWhiteSpace(commandQueueName))
            {
                throw new InvalidOperationException("Service Bus command queue name ('ServiceBus:CommandQueueName') is not configured.");
            }

            if (string.IsNullOrWhiteSpace(readRequestQueueName))
            {
                throw new InvalidOperationException("Service Bus read request queue name ('ServiceBus:ReadRequestQueueName') is not configured.");
            }

            if (string.IsNullOrWhiteSpace(readReplyQueueName))
            {
                throw new InvalidOperationException("Service Bus read reply queue name ('ServiceBus:ReadReplyQueueName') is not configured.");
            }

            _readReplyQueueName = readReplyQueueName;
            _commandSender = _serviceBusClient.CreateSender(commandQueueName);
            _readRequestSender = _serviceBusClient.CreateSender(readRequestQueueName);
        }

        public async Task SendBookCommandAsync(BookCommandMessage message, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(message);

            var serviceBusMessage = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                Subject = message.Action.ToString(),
                MessageId = message.MessageId.ToString()
            };

            await _commandSender.SendMessageAsync(serviceBusMessage, cancellationToken);

            _logger.LogInformation(
                "Book command published. Action: {Action}, BookId: {BookId}, MessageId: {MessageId}",
                message.Action,
                message.Id,
                message.MessageId);
        }

        public async Task<BookReadResponseMessage> SendBookReadRequestAsync(BookReadRequestMessage request, CancellationToken cancellationToken = default)
        {
            var requestMessage = new ServiceBusMessage(JsonSerializer.Serialize(request))
            {
                ContentType = "application/json",
                Subject = request.Action.ToString(),
                ReplyTo = _readReplyQueueName
            };

            await _readRequestSender.SendMessageAsync(requestMessage, cancellationToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ReadReplyTimeout);
            var deadline = DateTimeOffset.UtcNow.Add(ReadReplyTimeout);

            await using var receiver = _serviceBusClient.CreateReceiver(_readReplyQueueName);

            try
            {
                while (!timeoutCts.IsCancellationRequested)
                {
                    var remaining = deadline - DateTimeOffset.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                    {
                        break;
                    }

                    var messages = await receiver.ReceiveMessagesAsync(
                        maxMessages: 10,
                        maxWaitTime: remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero,
                        cancellationToken: timeoutCts.Token);

                    foreach (var replyMessage in messages)
                    {
                        var response = JsonSerializer.Deserialize<BookReadResponseMessage>(replyMessage.Body.ToString());

                        if (response?.CorrelationId == request.CorrelationId)
                        {
                            await receiver.CompleteMessageAsync(replyMessage, timeoutCts.Token);
                            return response;
                        }

                        await receiver.AbandonMessageAsync(replyMessage, cancellationToken: timeoutCts.Token);
                    }
                }

                throw new TimeoutException($"No reply received for read request {request.CorrelationId} within {ReadReplyTimeout.TotalSeconds} seconds.");
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Timed out waiting for a reply to read request {request.CorrelationId}.");
            }
        }
    }
}