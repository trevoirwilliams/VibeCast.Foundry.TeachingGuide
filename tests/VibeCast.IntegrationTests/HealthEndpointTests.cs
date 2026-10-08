using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VibeCast.IntegrationTests;

[TestClass]
public sealed class HealthEndpointTests
{
    private readonly Dictionary<string, string?> _previousEnvironment = new();

    // Minimal-hosted WebApplicationBuilder accesses Configuration before
    // WebApplicationFactory's late ConfigureAppConfiguration callback.
    // Supply startup prerequisites before CreateClient starts the host.
    [TestInitialize]
    public void PrepareTestEnvironment()
    {
        var settings = new Dictionary<string, string?>
        {
            ["DOTNET_ENVIRONMENT"] = "Testing",
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["ConnectionStrings__VibeCast"] =
                "Host=localhost;Database=vibecast;Username=postgres",
            ["Foundry__ProjectEndpoint"] = "https://example.openai.azure.com/",
            ["Foundry__ChatModelDeployment"] = "test-chat",
            ["Foundry__ImageModelDeployment"] = "test-image",
            ["Speech__Endpoint"] = "https://example.speech.azure.com/",
            ["ContentUnderstanding__Endpoint"] =
                "https://example.services.ai.azure.com/",
            ["KnowledgeStorage__ServiceUri"] =
                "https://example.blob.core.windows.net/",
            ["KnowledgeStorage__SearchEndpoint"] =
                "https://example.search.windows.net/",
            ["Registration__Enabled"] = "false"
        };

        foreach (var pair in settings)
        {
            _previousEnvironment[pair.Key] =
                Environment.GetEnvironmentVariable(pair.Key);
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }
    }

    [TestCleanup]
    public void RestoreTestEnvironment()
    {
        foreach (var pair in _previousEnvironment)
        {
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

    [TestMethod]
    public async Task HealthAndLivenessEndpoints_AreAvailableOutsideDevelopment()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var health = await client.GetAsync("/health");
        using var alive = await client.GetAsync("/alive");

        health.EnsureSuccessStatusCode();
        alive.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task PublicSelfRegistration_IsDisabledByDefault()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/Account/Register");

        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
