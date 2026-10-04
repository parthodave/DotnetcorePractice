# Application architecture

- The application is an ASP.NET Core Web API targeting .NET 8 with EF Core 8 and SQL Server.
- `Controllers/BooksController.cs` exposes the Books API through `IBookService`.
- `Application/Books/BookService.cs` coordinates book reads and writes through `IServiceBusService`.
- Book writes are sent to Azure Service Bus and applied by `BookCommandConsumerService`; read requests are handled by `BookReadRequestConsumerService`. The HTTP and AI layers must not write book rows directly.
- `POST /api/Ai/ask` is handled by `AiController` and `IAiService`. The AI layer uses the OpenAI .NET SDK Responses API and executes only registered application tools.
- `GET /health` uses the reusable application health service to verify SQL connectivity and returns HTTP 200 or 503.
- Serilog and Application Insights are the existing logging and telemetry infrastructure. Key Vault configuration is loaded by the existing application startup path when a vault URI is configured.
