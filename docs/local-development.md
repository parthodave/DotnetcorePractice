# Local development prerequisites

The Development launch profile uses `appsettings.Development.json`. It disables the Key Vault provider for that environment so local configuration (User Secrets or environment variables) can supply local credentials without an existing Azure Key Vault value overriding them. The configured vault URI is retained; other environments continue to load Key Vault by default when a vault URI is configured. Set `KeyVault:Enabled` to `true` to opt into Key Vault in Development.

## Required for startup

- Provide `ConnectionStrings:DefaultConnection` through Visual Studio User Secrets or an environment variable. It must point to a SQL Server instance reachable from this machine and have permission to create/apply the existing EF Core migrations. Development startup runs `Database.Migrate()`. Do not use the current cloud SQL connection if its firewall rejects this client's IP; this code does not change Azure SQL firewall rules.
- Launch with the existing `http` or `https` profile so `ASPNETCORE_ENVIRONMENT=Development` loads the development settings. Those settings already contain the Service Bus namespace and queue names; the application throws during configuration if the namespace is absent or not a fully qualified host name.

## Existing integrations and feature prerequisites

- The existing hosted consumers and book messaging continue to use the configured Azure Service Bus namespace and `DefaultAzureCredential`. To use them, provide a supported local credential and ensure it has the required access to the already-existing queues. No emulator, fake transport, queue creation, or Azure resource changes are performed by this application startup.
- Key Vault remains enabled by default outside Development. If enabled locally, the signed-in identity must be able to read the existing vault secrets.
- AI requests require valid configuration for the selected existing model provider. Application Insights/Azure Monitor queries require their existing workspace configuration and identity access. Missing optional AI/telemetry credentials do not represent verified integrations.

Keep connection strings and API keys out of committed JSON files. Use Visual Studio's User Secrets for `ConnectionStrings:DefaultConnection` and any local model credentials. An API/Swagger startup is only verified when it reaches the listening state; database health, message processing, and AI calls require their respective dependencies to be reachable and authorized.