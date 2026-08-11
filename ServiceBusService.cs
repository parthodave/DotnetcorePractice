using Azure.Identity;
using Azure.Messaging.ServiceBus;
using DotNet8WebAPI.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DotNet8WebAPI
{
    public class ServiceBusService : IServiceBusService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusSender _sender;
        private readonly ILogger<ServiceBusService> _logger;

        public ServiceBusService(
            ServiceBusClient serviceBusClient,
            IConfiguration configuration,
            ILogger<ServiceBusService> logger)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;

            var queueName = configuration["ServiceBus:QueueName"];

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new InvalidOperationException(
                    "Service Bus queue name is not configured.");
            }

            _sender = _serviceBusClient.CreateSender(queueName);
        }

        public async Task SendBookCreatedAsync(BookCreatedMessage message)
        {
            var json = JsonSerializer.Serialize(message);

            var serviceBusMessage = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                Subject = "BookCreated",
                MessageId = message.MessageId.ToString()
            };

            await _sender.SendMessageAsync(serviceBusMessage);

            _logger.LogInformation(
                "BookCreated message published successfully. BookId: {BookId}, MessageId: {MessageId}",
                message.BookId,
                message.MessageId);
        }
    }
}