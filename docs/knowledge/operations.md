# Health and telemetry operations

- `GET /health` executes a database connectivity check using SQL `SELECT 1` and reports health, timestamp, environment, and version.
- `get_application_health` returns the structured result from the same application health service. Database exception details stay in application logs and are not returned to the model.
- `get_recent_application_errors` queries existing workspace-based Application Insights data through Azure Monitor Logs Query only when `ApplicationInsights:WorkspaceId` is configured and the process identity is authorized.
- Error results are bounded to a configured time window and a maximum result count. Missing configuration or query failures are reported as `Unavailable`, not simulated as empty telemetry.
- `search_application_knowledge` retrieves short excerpts from the Markdown documents in this directory and returns their relative paths as citations. It does not call an Azure AI Search or vector-store resource.
