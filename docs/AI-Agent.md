# AI Agent in DotNet8WebAPI

## What is implemented

The existing `POST /api/Ai/ask` endpoint now runs a bounded multi-tool agent using the existing OpenAI .NET SDK 2.14.0 Responses API. The agent does not access EF Core, SQL, Service Bus, or telemetry directly. It only sees the explicit function schemas exposed by `IAgentToolExecutor`; C# services execute allowed calls.

No project, database schema, Service Bus contract, or Azure resource was created or changed for this feature. No Azure deployment was performed. The Azure Monitor query package is used only to read an already-configured workspace when the recent-errors tool is called and a workspace ID is configured.

## Request and tool flow

```text
POST /api/Ai/ask
  -> AiController (validates request and forwards X-Correlation-ID/request cancellation)
  -> IAiService / AiService (message limit, timeout, tool/iteration limits, AI telemetry)
  -> IAiModelClient
  -> OpenAiResponsesModelClient (OpenAI Responses SDK 2.14.0)
  -> registered FunctionCallResponseItem
  -> AgentToolExecutor (allow-list, JSON validation, execution policy)
  -> application service (Books, health, recent errors, or knowledge)
  -> typed result serialized as JSON and returned as FunctionCallOutputResponseItem
  -> model may request another tool or return final text
```

The configured provider is selected with `Agent:Provider`:

- `OpenAI`: uses `OpenAI:ApiKey` and `OpenAI:Model`; optional `OpenAI:Endpoint` supports an OpenAI-compatible endpoint.
- `AzureOpenAI`: uses `AzureOpenAI:ApiKey`, `AzureOpenAI:Endpoint`, and `AzureOpenAI:DeploymentName`. The configured Azure endpoint must support the OpenAI Responses API and include its compatible `/openai/v1/` path as appropriate. The deployment name is sent as the model value.

The orchestration remains Responses-specific behind `IAiModelClient` so another compatible implementation can be added later without replacing the tool execution layer. The model and deployment names are configuration values; this change does not select, deploy, or provision a model.

## Registered tools

| Tool | Policy | Implementation |
| --- | --- | --- |
| `get_all_books` | ReadOnly | Calls the existing `IBookService.GetAllBooks` read path, which continues to use the existing Service Bus read request/reply architecture. |
| `get_application_health` | ReadOnly | Reuses `IApplicationHealthService` and the existing SQL `SELECT 1` database check. Results contain status, database status, UTC timestamp, environment, version, and a safe message. |
| `get_recent_application_errors` | ReadOnly | Queries real Application Insights workspace logs using Azure Monitor Query. Missing configuration or query failure returns `Unavailable`; it never fabricates telemetry. |
| `search_application_knowledge` | ReadOnly | Searches the app-owned Markdown files under `docs/knowledge` and returns up to three short excerpts with relative-path citations. |

Book writes remain on the existing Books API -> `IBookService` -> Service Bus -> consumer -> EF Core path. No book CRUD AI tools were added.

## Multi-tool control, guardrails, and safety

- Health requests are instructed to call `get_application_health`; recent-error requests use the telemetry tool; an unhealthy diagnosis can call health and then recent errors.
- Every requested function is checked against the C# tool registry. Unknown functions are rejected safely.
- `ToolExecutionPolicy.ReadOnly` allows the four registered read tools. `RequiresConfirmation` returns a `PendingApproval` result with the tool-call ID as an approval ID and does not run the delegate. There is no destructive tool or approval endpoint in this change; a future caller would need an explicit approval workflow before executing such actions.
- The agent limits message size (`Agent:MaxMessageLength`), tool rounds (`Agent:MaxToolIterations`), tool calls (`Agent:MaxToolCalls`), and wall-clock execution (`Agent:TimeoutSeconds`). Values are clamped to safe ranges in code.
- Cancellation is passed from the HTTP request through model calls, database health checks, and the existing book read Service Bus call.
- Tool arguments are JSON-validated. Tool and provider exceptions are logged internally and safe structured results are sent to the model. User content, authorization headers, API keys, and connection strings are not logged by the agent.
- Recent telemetry results are capped and common credential-shaped values are redacted before model exposure.

## Health tool

The existing `GET /health` route continues to return HTTP 200 for a healthy database and 503 for an unhealthy database, using the same health service. Its response field remains `timestamp` for compatibility. The AI tool result uses `timestampUtc` and includes `databaseStatus`; raw SQL exception details are kept in logs and never returned to the model.

## Recent application errors

The implementation uses `Azure.Monitor.Query` and `DefaultAzureCredential` to query an already-existing Log Analytics workspace. It queries exceptions, failed requests, failed dependencies, and error-level traces from the workspace-based `AppExceptions`, `AppRequests`, `AppDependencies`, and `AppTraces` tables. It runs a bounded read-only batch query and returns a configured window (default 30 minutes), the total matching error count, at most 20 results by default, and a truncation indicator.

Required setting:

- `ApplicationInsights:WorkspaceId` — the workspace ID containing this application's workspace-based Application Insights data.

Optional settings:

- `ApplicationInsights:ConnectionString` — ingestion/SDK configuration for the existing Application Insights resource; this is not used as a query credential.
- `ApplicationInsights:WindowMinutes` — default 30; clamped from 1 to 120.
- `ApplicationInsights:MaxResults` — default 20; clamped from 1 to 25.

The process identity must already have read/query permission on that workspace. No workspace is created, modified, or assigned a role by this implementation. If the workspace ID is absent, the result is `Unavailable` with an explicit configuration message. Authentication, permission, schema, or query errors are logged internally and returned as safe `Unavailable` results.

Leave `ApplicationInsights:WorkspaceId` empty to guarantee this tool does not query Azure Monitor. When enabled, its use is subject to the existing workspace's access, retention, quota, and billing configuration; this change does not enable a service or change a pricing tier.

## Knowledge retrieval (RAG)

`IKnowledgeService` currently uses a lightweight local Markdown retrieval implementation. It tokenizes the question, ranks matching paragraphs, and returns up to three excerpts (maximum 1,000 characters each) with repository-relative citations. It reads only `.md` files from the configured knowledge directory, skips files over 256 KB, and caps processing at 100 files. Current source docs are in `docs/knowledge/`.

This is real file-based retrieval, not vector/embedding semantic search. It requires no Azure AI Search, OpenAI vector store, or other hosted service. `Knowledge:Directory` can point to a different local directory. Missing directories and empty sources return an explicit `Unavailable` result. A future implementation can replace `IKnowledgeService` with Azure AI Search or hosted file search without changing the agent's function-call loop.

## Observability

The agent uses the existing `ILogger`, Serilog log context, and `TelemetryClient`. It emits:

- `AI.Agent.Started`
- `AI.Agent.ToolCalled`
- `AI.Agent.ToolSucceeded`
- `AI.Agent.ToolFailed`
- `AI.Agent.Completed`
- `AI.Agent.Failed`

Events contain request/correlation ID, provider/model, tool name, call count, duration, result status, and outcome where relevant. They do not contain the full user prompt. Application health continues to emit `HealthCheckPassed` and `HealthCheckFailed`.

## Local configuration

The non-secret defaults and empty placeholders are in `appsettings.json`. Do not place credentials in source control. Use User Secrets for local development, for example from the Web API project directory:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "<your-existing-openai-api-key>"
dotnet user-secrets set "OpenAI:Model" "<your-existing-model-name>"
```

For an existing Azure OpenAI endpoint instead:

```powershell
dotnet user-secrets set "Agent:Provider" "AzureOpenAI"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<key-from-your-existing-secret-store>"
dotnet user-secrets set "AzureOpenAI:Endpoint" "<your-existing-responses-compatible-endpoint>"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "<your-existing-deployment-name>"
```

To enable recent-error lookup against an existing workspace:

```powershell
dotnet user-secrets set "ApplicationInsights:WorkspaceId" "<existing-workspace-id>"
dotnet user-secrets set "ApplicationInsights:WindowMinutes" "30"
dotnet user-secrets set "ApplicationInsights:MaxResults" "20"
```

Secrets already resolved through the repository's Key Vault configuration remain supported. For Azure hosting, configure the existing app identity/credential chain to read its already-existing Key Vault secrets and assign only the required read/query access to an existing Log Analytics workspace. These are optional operator-managed prerequisites; this code change performs no role assignments or infrastructure changes.

With the existing Key Vault configuration provider, hierarchical configuration keys can be represented in secret names with `--` as the separator, for example `OpenAI--ApiKey`, `AzureOpenAI--ApiKey`, or `ApplicationInsights--WorkspaceId`. Keep API keys in User Secrets or the already-configured Key Vault, not in `appsettings.json`.

Agent limits can be adjusted with configuration:

```text
Agent:MaxMessageLength = 4000
Agent:MaxToolIterations = 6
Agent:MaxToolCalls = 10
Agent:TimeoutSeconds = 60
```

## Run and verify locally

From the solution directory:

```powershell
dotnet restore .\DotNet8WebAPI.sln
dotnet build .\DotNet8WebAPI.sln
dotnet test .\DotNet8WebAPI.sln
dotnet run --project .\DotNet8WebAPI.csproj
```

Open the Swagger URL printed by the app, usually `https://localhost:<port>/swagger`. The existing application requires its configured SQL, Service Bus, and Key Vault dependencies to start; no new Azure resources are required or created by this feature. Running a real AI request also requires valid provider configuration and may incur the existing model-provider usage charges.

`POST /api/Ai/ask` accepts a JSON string body, for example:

```json
"Is my application healthy?"
```

Also test:

```json
"Are there any recent application errors?"
```

```json
"Why is my application unhealthy?"
```

```json
"Is my application healthy and are there any recent errors?"
```

```json
"Show me my books."
```

```json
"Explain dependency injection."
```

For health, error, book, or repository questions, inspect the application logs for the corresponding `ToolName`. The final answer is model-generated; exact wording is not deterministic. If no workspace ID is configured, the real-error tool should explicitly report `Unavailable`, not claim that there were no errors.

## Automated evaluation

`DotNet8WebAPI.Tests/AiAgentTests.cs` uses scripted model turns and fake dependencies to verify health/error/book routing, sequential multi-tool execution, normal-question behavior, safe tool failure handling, loop protection, confirmation blocking, and missing telemetry configuration. These tests do not call OpenAI, SQL, Service Bus, or Azure Monitor.

The scripted tests verify C# dispatch and safety, not the live model's intent classification. Live tool selection and integration require an operator-provided model configuration and existing application dependencies.

## Interview summary

The model proposes function calls but has no direct infrastructure access. An allow-listed C# executor validates the request, enforces the tool policy, invokes existing application services, serializes bounded structured facts, and returns tool output to the Responses API. The loop is bounded and observable. Books retain their messaging architecture; health reuses the shared health service; errors query only configured real telemetry; and RAG supplies cited local documentation without requiring a new hosted search resource.
