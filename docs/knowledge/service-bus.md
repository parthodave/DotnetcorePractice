# Books and Service Bus flow

- Book API reads call `IBookService`, which sends a read request through `IServiceBusService` and awaits the correlated reply from the read consumer.
- Book create and update requests are published as `BookCommandMessage` commands. The command consumer applies those changes to EF Core and SQL Server.
- Delete operations also use the existing book command flow; the AI agent exposes no write or delete tool.
- The agent's `get_all_books` tool calls the existing `IBookService.GetAllBooks()` method. It does not access `OurHeroDbContext` or bypass Service Bus.
- Service Bus namespace and queue names are supplied by application configuration. The existing consumers require the configured command, read-request, and read-reply queue names.
