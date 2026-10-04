#pragma warning disable OPENAI001

using OpenAI.Responses;
using System.ClientModel;

namespace DotNet8WebAPI.Services.AI;

public sealed class OpenAiResponsesModelClient : IAiModelClient
{
    private readonly ResponsesClient _client;
    private readonly ILogger<OpenAiResponsesModelClient> _logger;

    public OpenAiResponsesModelClient(
        IConfiguration configuration,
        Microsoft.Extensions.Options.IOptions<AiAgentOptions> agentOptions,
        ILogger<OpenAiResponsesModelClient> logger)
    {
        _logger = logger;
        var provider = agentOptions.Value.Provider.Trim();

        string apiKey;
        string? endpoint;

        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            ProviderName = "OpenAI";
            apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
            endpoint = configuration["OpenAI:Endpoint"];
            ModelName = configuration["OpenAI:Model"] ?? string.Empty;
        }
        else if (provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            ProviderName = "AzureOpenAI";
            apiKey = configuration["AzureOpenAI:ApiKey"] ?? string.Empty;
            endpoint = configuration["AzureOpenAI:Endpoint"];
            ModelName = configuration["AzureOpenAI:DeploymentName"] ?? string.Empty;
        }
        else
        {
            throw new InvalidOperationException(
                "Agent:Provider must be either 'OpenAI' or 'AzureOpenAI'.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"An API key for the configured {ProviderName} provider is not configured.");
        }

        if (string.IsNullOrWhiteSpace(ModelName))
        {
            var modelSetting = ProviderName == "OpenAI"
                ? "OpenAI:Model"
                : "AzureOpenAI:DeploymentName";
            throw new InvalidOperationException(
                $"The configured model or deployment name is missing: {modelSetting}.");
        }

        var clientOptions = new ResponsesClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
                (endpointUri.Scheme != Uri.UriSchemeHttps && !endpointUri.IsLoopback))
            {
                throw new InvalidOperationException(
                    $"The configured {ProviderName} endpoint must be an absolute HTTPS URI.");
            }

            clientOptions.Endpoint = endpointUri;
        }
        else if (ProviderName == "AzureOpenAI")
        {
            throw new InvalidOperationException(
                "AzureOpenAI:Endpoint must be configured for the AzureOpenAI provider.");
        }

        _client = new ResponsesClient(new ApiKeyCredential(apiKey), clientOptions);
        _logger.LogInformation(
            "Configured AI model client for provider {Provider} and model {Model}",
            ProviderName,
            ModelName);
    }

    public string ProviderName { get; }
    public string ModelName { get; }

    public async Task<AiModelTurn> CreateResponseAsync(
        IReadOnlyList<ResponseItem> inputItems,
        IReadOnlyList<ResponseTool> tools,
        CancellationToken cancellationToken)
    {
        var options = new CreateResponseOptions(ModelName, inputItems);
        foreach (var tool in tools)
        {
            options.Tools.Add(tool);
        }

        var response = await _client.CreateResponseAsync(options, cancellationToken);
        return new AiModelTurn(
            response.Value.OutputItems.ToArray(),
            response.Value.GetOutputText());
    }
}
