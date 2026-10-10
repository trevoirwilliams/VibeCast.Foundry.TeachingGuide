using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VibeCast.Infrastructure.Options;

namespace VibeCast.Infrastructure.Authentication;

public static class AzureCredentialFactory
{
    public static TokenCredential Create(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return new DefaultAzureCredential();
        }

        string? clientId = configuration[$"{AzureIdentityOptions.SectionName}:ManagedIdentityClientId"];

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("AzureIdentity:ManagedIdentityClientId is required in Production.");
        }

        if (!Guid.TryParse(clientId, out _))
        {
            throw new InvalidOperationException("AzureIdentity:ManagedIdentityClientId must be a valid GUID.");
        }

        ManagedIdentityId identity = ManagedIdentityId.FromUserAssignedClientId(clientId);

        return new ManagedIdentityCredential(identity);
    }
}
