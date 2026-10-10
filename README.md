# VibeCast — Section 9 backend practice

**Branch:** `section-09-prod-prep-practice`

**Target:** [Section 9 Complete at c281561](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/c2815618038f285631c54fee3a5e42ce80e7b66f)

Practice the backend changes from the published **Deploy the AI-Enabled .NET Application to Azure** section. Supporting code matches the target checkpoint; seven exercises leave selected implementation blocks for you to complete.

Start with the [practice guide](docs/practice/README.md). Search C# files for `PRACTICE S09-`. Each gap states the required sequence, expected result and an optional hint. The scaffold should compile; named `NotImplementedException` failures identify unfinished work.

## Activities and lesson order

| Published lesson | Student activity |
|---|---|
| Clean Up and Stabilize for Production | Inspect the supplied resilience/logging chain and shared telemetry registration. |
| Move Relational Data to PostgreSQL for Production | **S09-04:** configure the local PostgreSQL options callback. |
| Implement Azurite for Development File Storage | **S09-01–03:** save, open and delete media blobs. |
| Complete Azurite Implementation and Test | **S09-05A/B:** select the development key-ring blob; configure production persistence and Key Vault protection. |
| Normalize Container Configurations and Run Locally | **S09-06:** resolve the keyed media container; configure AppHost and inspect uploaded files. |
| Provision Azure Infrastructure | Follow the published provisioning activity using your Azure resources. |
| Publish and Deploy VibeCast to Azure Container Apps | Configure the completed application for Production and verify its behavior. |
| Add Application Insights and Production Health Monitoring | **S09-07:** connect the production exporter and verify traces separately from health. |

S09-01–03 keep their original IDs for existing students. Complete **S09-04, S09-05A and S09-06** before launching the full local app; implement the storage methods before uploading media. S09-05B and S09-07 are Production-only gaps.

## Local setup

Use **.NET 10 / C# 14**, the committed package versions and a Docker-compatible runtime. [AppHost](VibeCast.AppHost/AppHost.cs) creates PostgreSQL and Azurite with persistent volumes, then builds the web Dockerfile. It selects Development and injects database/storage configuration. Use the web URL shown by AppHost (normally localhost:8080).

Configure the **AppHost project's** user secrets. Its parameters are:

- `Parameters:foundry-project-endpoint`
- `Parameters:foundry-chat-model-deployment`
- `Parameters:foundry-image-model-deployment`
- `Parameters:foundry-api-key`
- `Parameters:speech-endpoint`
- `Parameters:speech-api-key`
- `Parameters:content-understanding-endpoint`
- `Parameters:content-understanding-api-key`
- `Parameters:knowledge-storage-service-uri`
- `Parameters:knowledge-search-endpoint`
- `Parameters:azure-tenant-id`
- `Parameters:azure-client-id`
- `Parameters:azure-client-secret`

The last three supply a development service principal to the local web container as AZURE_TENANT_ID, AZURE_CLIENT_ID and AZURE_CLIENT_SECRET. That identity needs access to the configured knowledge storage/search resources. The container does not inherit the host's Visual Studio/Azure CLI login or the web project's user secrets. Keep credentials outside committed files.

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet run --project VibeCast.AppHost
```

Development startup migrates and seeds PostgreSQL. Keep the supplied PostgreSQL migration history; do not repeat the recording's earlier SQLite migration deletion. [SeedData.cs](src/VibeCast.Infrastructure/Data/SeedData.cs) defines the local teaching account.

## Production code supplied in this checkpoint

The branch includes shared managed-identity credentials, authenticated PostgreSQL/AI client registrations, production media storage options, [appsettings.Production.json](src/VibeCast.Web/appsettings.Production.json), and [the initial schema script](vibecast-initial-schema.sql).

Configure your deployed application using the actual resource values:

| Configuration | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT=Production` | Select production registrations. |
| `AzureIdentity__ManagedIdentityClientId` | Client ID of the attached user-assigned identity. |
| `ConnectionStrings__VibeCast` | PostgreSQL connection string for the mapped runtime database principal. |
| `MediaStorage__ServiceUri`, `MediaStorage__ContainerName` | Production media storage endpoint/container. |
| `DataProtection__BlobUri`, `DataProtection__KeyVaultKeyIdentifier` | Key-ring blob and Key Vault key identifier. |
| Foundry, Speech, ContentUnderstanding and KnowledgeStorage sections | Existing AI endpoints, deployments and retrieval settings; see the [options classes](src/VibeCast.Infrastructure/Options). |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Destination for the production telemetry exporter. |

Environment variables use double underscores for configuration nesting. The production JSON file contains defaults, not all required deployment settings. Production uses managed identity rather than the local container's service-principal secret. Azure permissions and the PostgreSQL principal/table permissions must already be configured.

Production startup does not run migrations or seed data. Apply the schema and required database setup separately using the published deployment instructions. This snapshot does not contain all infrastructure assets referenced by recordings; completing the code is not evidence that Azure resources have been provisioned.

## Verification

See the [task-specific checks](docs/practice/README.md#build-and-check). Compile first, then verify storage roundtrips, the actual media container, persistence across restarts and production telemetry.

The existing health integration-test fixture lacks PostgreSQL/storage setup; launching AppHost separately does not automatically supply it. That inherited test failure is separate from a named unfinished exercise. Domain/application tests do not cover all Section 9 tasks.

The health endpoints currently expose a self-check. Verify a real database/storage/AI operation separately; a healthy process alone does not establish dependency access.

## Related checkpoints

- [Start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-start)
- [Complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-complete)
- [Pinned solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/c2815618038f285631c54fee3a5e42ce80e7b66f)
