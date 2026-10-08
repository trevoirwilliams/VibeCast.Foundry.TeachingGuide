using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VibeCast.IntegrationTests;

[TestClass]
public sealed class HealthEndpointTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Testing exercises production-safe routing without invoking
                // Development startup migrations or Azure production credentials.
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:VibeCast"] =
                            "Host=localhost;Database=vibecast;Username=postgres",
                        ["Foundry:ProjectEndpoint"] =
                            "https://example.openai.azure.com/",
                        ["Foundry:ChatModelDeployment"] = "test-chat",
                        ["Foundry:ImageModelDeployment"] = "test-image",
                        ["Speech:Endpoint"] = "https://example.speech.azure.com/",
                        ["ContentUnderstanding:Endpoint"] =
                            "https://example.services.ai.azure.com/",
                        ["KnowledgeStorage:ServiceUri"] =
                            "https://example.blob.core.windows.net/",
                        ["KnowledgeStorage:SearchEndpoint"] =
                            "https://example.search.windows.net/",
                        ["Registration:Enabled"] = "false"
                    });
                });
            });

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
