# VibeCast — Deploy VibeCast to Azure Container Apps

**Teaching checkpoint:** `section-09-container-apps-complete`  
**Starting branch:** [section-09-prod-prep-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-complete)  
**Runtime:** .NET 10 / C# 14 / ASP.NET Core Blazor Interactive Server

The deployment lesson takes VibeCast from a local Aspire/Docker environment to its first externally reachable Azure Container Apps revision. It reuses the provisioned Azure Container Registry, Container Apps environment, Microsoft Entra runtime identity, PostgreSQL Flexible Server, Blob Storage, Key Vault, Microsoft Foundry, and Azure AI Search.

## Start here

**[Complete step-by-step deployment and recording guide](docs/deploy-container-apps.md)**

From the repository root, after authenticating with the intended Azure subscription:

```powershell
git switch section-09-container-apps-complete

# Approve and verify the initial schema, application reference data and runtime DML:
.\infra\bootstrap-production-database.ps1

# Then build/publish the versioned image and apply a reviewed Container App deployment:
.\infra\deploy-vibecast-containerapp.ps1 -DatabaseBootstrapVerified
```

The deployment script prompts for required nonsecret endpoints and deployment names when they are not already supplied through parameters or environment variables. No production provider API keys are required. Both scripts have explicit human approval gates and are intended to be safe to rerun.

| Artifact | Responsibility |
|---|---|
| [infra/bootstrap-production-database.ps1](infra/bootstrap-production-database.ps1) | Generate/apply initial EF Core schema, seed required policy, grant runtime data privileges, verify and remove temporary firewall access |
| [infra/seed-production-reference-data.sql](infra/seed-production-reference-data.sql) | Idempotent default policy initialization; no demonstration users |
| [infra/container-app.bicep](infra/container-app.bicep) | One cost-controlled public HTTPS Container App, UAMI, ACR pull, health probes and production environment variables |
| [infra/deploy-vibecast-containerapp.ps1](infra/deploy-vibecast-containerapp.ps1) | ACR release image, Bicep what-if/deploy, public health and login-page checks |
| [docs/deploy-container-apps.md](docs/deploy-container-apps.md) | Exact setup, optional brief demo-account registration, authenticated episode smoke check and troubleshooting |

## Completion boundary

The release is complete only after the app returns healthy over HTTPS, an intended demo account can sign in, and a newly created episode remains in PostgreSQL after reload and reauthentication. The deployment script cannot automate the authenticated episode smoke test; complete it manually before recording the success checkpoint.

Public registration is **disabled by default**. To prepare an initial demo account, the deployment script supports an explicitly approved temporary registration mode that must be disabled immediately afterward.

The initial app uses **min 0 / max 1** replicas to minimize idle compute costs. Cold starts are expected; in-memory background jobs are not durable. Persistent data is stored outside the container.

## Scope intentionally deferred

Full Foundry model inference, Speech/Content Understanding, Blob read/write/delete, Foundry IQ retrieval, OpenTelemetry export to Application Insights, rollbacks, and CI/CD deployment are addressed in subsequent Section 9 lessons. Local Aspire remains supported by the previous teaching branches and AppHost.

For technical policy and migration constraints, see the checked-in `.github/instructions` and [Microsoft Learn: Azure Container Apps](https://learn.microsoft.com/azure/container-apps/dotnet-overview).
