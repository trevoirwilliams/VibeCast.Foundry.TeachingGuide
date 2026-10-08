# VibeCast — Deploy to Azure Container Apps

**Lesson:** Deploy VibeCast to Azure Container Apps  
**Teaching branch:** `section-09-container-apps-complete`  
**Starting point:** `section-09-prod-prep-complete`

This checkpoint deploys **only VibeCast.Web** to the Container Apps environment already provisioned by Section 9 Bicep. PostgreSQL, Blob Storage, Microsoft Foundry, Azure AI Search, Key Vault and ACR remain managed Azure dependencies, not containers in the production workload. The Aspire AppHost remains for local development.

## Deployment order (Windows PowerShell)

Start in the cloned repository's root on `section-09-container-apps-complete`. Use the PowerShell window where `az login` selected the intended Azure tenant and subscription. Install the .NET 10 SDK, Docker Desktop, Azure CLI with its Container Apps extension, Bicep support, and the `dotnet-ef` tool matching the checked-in EF Core major version. The existing Docker-based PostgreSQL client requires no local PostgreSQL installation.

Verify the Azure subscription before approving any changes:

```powershell
az account show --query "{subscription:name,subscriptionId:id,tenantId:tenantId}" --output table
git switch section-09-container-apps-complete
dotnet restore VibeCast.sln
dotnet test VibeCast.sln --configuration Release
```

### 1. Bootstrap the empty PostgreSQL database (separate approval)

The earlier identity-bootstrap script created `id-vibecast-prod` and granted **CONNECT** and **USAGE**, but did not create tables or grant runtime DML. Before deploying the app:

```powershell
.\infra\bootstrap-production-database.ps1
```

The script:

1. Reads live infrastructure resource IDs and the PostgreSQL identity from `vibecast-production-prerequisites`.
2. Builds/tests the current .NET solution and generates an **idempotent EF Core migration SQL script**, rather than running `MigrateAsync()` in production.
3. Creates a temporary exact-IP PostgreSQL firewall rule after explicit approval.
4. Uses the signed-in PostgreSQL **Microsoft Entra administrator** and a short-lived access token with Docker `postgres:18` / `psql`.
5. Verifies the existing runtime role's Entra principal ID.
6. Applies the EF Core schema, verifies migration `20261001044539_InitialPostgreSql`, and seeds the *required* default episode-format policy.
7. Grants DML only on the explicit application/Identity tables and the two claims sequences; grants read-only policy access, excludes migration history, and confirms no schema `CREATE` privilege.
8. Restores the token environment and removes the temporary firewall rule in `finally`.

Review the generated SQL and intended operations before entering `YES`. If a cleanup warning occurs, follow the printed exact firewall-rule delete command. No demo users or sample episodes are seeded. Re-running the script preserves preexisting data and is intended to be safe; investigate any reported schema drift rather than resetting the database.

### 2. Build and deploy with Bicep + PowerShell

```powershell
.\infra\deploy-vibecast-containerapp.ps1 -DatabaseBootstrapVerified
```

The script prompts for **non-secret**, existing Foundry chat/image deployment names and Foundry, Speech, and Content Understanding endpoints if they are not passed as parameters or provided in environment variables. It discovers the Container Apps environment, Azure Container Registry, managed identity, PostgreSQL connection configuration, Blob storage URIs, Key Vault key ID, and the existing `vibecast-search` endpoint. It requires a clean Git working tree, uses the current 12-character commit SHA as the release tag, validates/builds the Bicep, performs a Release build and tests, and uses `az acr build` to publish to the registry only if that commit tag is absent. It resolves the tag to a SHA-256 digest and deploys the digest reference so later tag changes cannot alter the selected image.

It then shows `az deployment group what-if` for `infra/container-app.bicep`, asks for explicit deployment approval, deploys, and checks public `/health` and `/Account/Login`. It does **not** silently recreate the foundation or apply schema changes.

If a script exits after publishing the image but before deploying the app, rerun it: the existing commit-tagged image can be reused. Image creation and deployment approvals are separate.

### 3. Provision one temporary demonstration account

Public self-registration is **disabled by default in Production**. No account password is stored in Git, a Bicep parameter, or an appsettings file.

For the initial lesson demonstration only, temporarily permit registration by running:

```powershell
$demoEmail = Read-Host 'Designated nonproduction demo email'
.\infra\deploy-vibecast-containerapp.ps1 -DatabaseBootstrapVerified -EnableRegistrationTemporarily -DemoAccountEmail $demoEmail
```

Open the public URL printed by the script, visit `/Account/Register`, and register an account using a nonproduction email address and a unique password. **Immediately** rerun the standard deployment command without the registration switch and verify `/Account/Register` returns HTTP **404**. Do not display the registration password in the recording.

Temporary registration fails closed without an allowed email and accepts only that email (case-insensitive). ASP.NET Core Identity prevents a second account with the same normalized email. This does not verify email ownership: keep the exposure window brief and disable registration immediately. Do not use this pattern as a permanent invitation system.

### 4. Record the initial hosted smoke test

In the Azure portal or through Azure CLI, confirm the deployed revision is Running and the managed identity is attached. Then:

1. Open the HTTPS URL; verify the `/health` and login page are reachable after a possible cold start.
2. Sign in with the designated demonstration account.
3. Create one VibeCast episode, save it, refresh the list, sign out, and sign back in to retrieve it.
4. Confirm the resulting episode persisted in **Azure Database for PostgreSQL**, not on the container filesystem.
5. Capture the Container App name, image tag, revision ID, and startup logs as the completion checkpoint.

The PowerShell deployment script **automates the unauthenticated health and login-page HTTP checks only**. It does not impersonate the user or claim a successful authenticated CRUD smoke test before the browser sequence is completed.

## Runtime contract

| Dimension | Initial implementation |
|---|---|
| Hosting | Existing `cae-vibecast-prod` environment in `rg-vibecast-prod` |
| Registry | Existing ACR, pull authenticated using `id-vibecast-prod` |
| Image | Root Dockerfile, .NET 10 Release, non-root runtime, port 8080 |
| External ingress | Public HTTPS; Container Apps handles TLS |
| Host environment | `Production`; no Development API keys |
| Authentication | ASP.NET Core Identity in PostgreSQL, with shared Blob/Data Protection Key Vault key ring |
| App data | PostgreSQL; runtime role has DML, not schema-admin privileges |
| Files | Azure Blob Storage; no durable container filesystem |
| Revisions | Single, session affinity enabled for Interactive Server |
| Scale | Minimum 0, maximum 1 replica; cold starts and local-job loss on scale-down are possible |
| Health | `/alive` and `/health` are app/self checks, *not* proof all external AI services are ready |
| Registration | Disabled in Production, temporary explicit opt-in for account preparation |

**Important limits:** The in-memory job queue does not offer durable background execution across scale-to-zero/restarts. Container Apps billing is minimized, not eliminated: PostgreSQL, ACR, Blob Storage, Key Vault and other Azure services can incur charges independently. Use real authorization, email verification, security and traffic controls before making VibeCast a permanent public production service.

## Troubleshooting

- **Unauthorized registry pull:** verify `id-vibecast-prod` has `AcrPull` at the existing registry scope, and the app has the corresponding user-assigned identity configured for pull.
- **Database permission error:** rerun/inspect the database bootstrap and confirm the schema migration history, DML grants, and Entra object-ID mapping. Do not make the runtime identity a PostgreSQL administrator.
- **No episode-format policy:** confirm `format-default-2026.1` exists and is active in `public."EpisodeFormatPolicies"`.
- **Cannot decrypt cookies / sign-in fails after restart:** verify the Data Protection Blob URI and Key Vault Key Crypto User role.
- **Redirect loop:** check the ingress-provided `X-Forwarded-Proto` and `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.
- **Repeated startup errors:** inspect console and system logs, config validation failures, outbound network policies, and startup/readiness probes.
- **Authentication succeeds but data isn't persistent:** confirm `ConnectionStrings__VibeCast` targets the Azure PostgreSQL server and not Aspire's local PostgreSQL.
- **Search/Foundry feature failures:** follow the subsequent **Connect and Validate Production Dependencies** lesson; they are not part of this first-deployment smoke contract.

```powershell
az containerapp show --name ca-vibecast-prod --resource-group rg-vibecast-prod --query "{FQDN:properties.configuration.ingress.fqdn,Identity:identity,Image:properties.template.containers[0].image}" --output json
az containerapp revision list --name ca-vibecast-prod --resource-group rg-vibecast-prod --output table
az containerapp logs show --name ca-vibecast-prod --resource-group rg-vibecast-prod --type console --follow
```

## Lesson boundary and recording flow

Proposed 12-minute recording: 45 seconds objective, 1 minute prerequisite state, 2 minutes versioned ACR build, 2 minutes Bicep and production configuration, 2 minutes deployment, 1.5 minutes health/revision check, 2 minutes sign-in and persisted episode, 45 seconds result/next milestone. Edit waiting intervals without concealing significant errors or setup changes.

**Next lessons:** Connect and Validate Production Dependencies; Add Application Insights and Production Health Monitoring; Deploy a New Revision and Roll Back; automate deployment with GitHub Actions. No CI/CD changes are introduced here.

## Microsoft documentation

- [Deploy container images to Azure Container Apps](https://learn.microsoft.com/azure/container-apps/tutorial-code-to-cloud)
- [Container Apps image pull using managed identities](https://learn.microsoft.com/azure/container-apps/managed-identity-image-pull)
- [Container Apps health probes](https://learn.microsoft.com/azure/container-apps/health-probes)
- [Blazor hosting in Azure Container Apps](https://learn.microsoft.com/aspnet/core/blazor/host-and-deploy/server/?view=aspnetcore-10.0#azure-container-apps)
- [Apply EF Core migrations in production](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)

## Verification status — 8 October 2026

Local Release build and 23 deterministic .NET tests passed. Bicep compilation/lint and PowerShell parsing passed. EF reports no pending model changes. The optional local PostgreSQL regression below passed against PostgreSQL 18, including two migration/seed/grant runs, preserved administrator edits, working claims-sequence DML, and denied schema/policy/migration-history writes. Existing compiler warnings in AI/UI code remain outside this deployment change.

```powershell
# Requires the Release build, dotnet-ef 10.0.12 and local Docker; makes no Azure calls.
.\scripts\test-production-database.ps1
```

**Not verified live:** Azure tenant/subscription access; foundation deployment outputs; Entra administrator access and identity mapping; database firewall/network paths from the workstation and Container Apps; runtime service RBAC and ACR ARM-token authentication; ACR Tasks build; Bicep what-if/validation and deployment; hosted probes, forwarded HTTPS/secure cookies and key-ring access; demo onboarding/registration closure; authenticated login and saved-episode persistence. No Azure mutation, billable build, or deployment was executed for this verification. Approve database changes, image publication, and app deployment separately when running the scripts.

The cloud forwarding flag trusts ingress-provided forwarding headers. This checkpoint relies on HTTP Container Apps ingress being the only external route to port 8080: Container Apps overwrites `X-Forwarded-Proto`, and the default one-hop processing avoids trusting arbitrary earlier client IP entries. Do not expose the container port directly outside that boundary. Production authentication and antiforgery cookies require HTTPS. Direct HTTP probes bypass redirection and return the health handler's status. Liveness/readiness in this lesson remain application/self checks, not external dependency readiness.

- [Container Apps ingress and forwarded header behavior](https://learn.microsoft.com/azure/container-apps/ingress-overview)
- [ASP.NET Core forwarding configuration and trust boundary](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- [PostgreSQL Microsoft Entra authentication](https://learn.microsoft.com/azure/postgresql/security/security-entra-configure)
