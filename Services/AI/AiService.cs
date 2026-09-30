#pragma warning disable OPENAI001

using DotNet8WebAPI.Application.Books;
using OpenAI.Responses;
using System.Text.Json;

namespace DotNet8WebAPI.Services.AI;

public class AiService : IAiService
{
    private readonly ResponsesClient _client;
    private readonly IConfiguration _configuration;
    private readonly IBookService _bookService;

    public AiService(
        IConfiguration configuration,
        IBookService bookService)
    {
        _configuration = configuration;
        _bookService = bookService;

        var apiKey = _configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenAI API key is not configured.");

        _client = new ResponsesClient(apiKey);
    }

    private static readonly FunctionTool GetAllBooksTool =
        ResponseTool.CreateFunctionTool(
            functionName: "get_all_books",
            functionDescription: "Gets all books from the database.",
            functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {},
                "additionalProperties": false
            }
            """),
            strictModeEnabled: true);

    public async Task<string> AskAsync(string message)
    {
        var model = _configuration["OpenAI:Model"]
            ?? throw new InvalidOperationException(
                "OpenAI model is not configured.");

        var inputItems = new List<ResponseItem>
        {
            ResponseItem.CreateDeveloperMessageItem(
                "You are an assistant for a .NET application. " +
                "When the user asks about books, use the available book tools. " +
                "Do not invent book data.")
        };

        inputItems.Add(
            ResponseItem.CreateUserMessageItem(message));

        while (true)
        {
            var options = new CreateResponseOptions(
                model,
                inputItems)
            {
                Tools =
                {
                    GetAllBooksTool
                }
            };

            var response =
                await _client.CreateResponseAsync(options);

            inputItems.AddRange(response.Value.OutputItems);

            var toolWasCalled = false;

            foreach (var outputItem in response.Value.OutputItems)
            {
                if (outputItem is not FunctionCallResponseItem functionCall)
                {
                    continue;
                }

                switch (functionCall.FunctionName)
                {
                    case "get_all_books":
                        {
                            var books = await _bookService.GetAllBooks();

                            var toolResult =
                                JsonSerializer.Serialize(books);

                            inputItems.Add(
                                new FunctionCallOutputResponseItem(
                                    functionCall.CallId,
                                    toolResult));

                            toolWasCalled = true;
                            break;
                        }

                    default:
                        throw new InvalidOperationException(
                            $"Unknown function: {functionCall.FunctionName}");
                }
            }

            if (!toolWasCalled)
            {
                return response.Value.GetOutputText();
            }
        }
    }
}