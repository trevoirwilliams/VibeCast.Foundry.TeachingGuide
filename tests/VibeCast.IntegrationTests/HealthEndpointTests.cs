using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VibeCast.IntegrationTests;

[TestClass]
public sealed class HealthEndpointTests
{
    [TestMethod]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Foundry:ProjectEndpoint"] = "https://example.openai.azure.com/",
                        ["Foundry:ChatModelDeployment"] = "test-chat-deployment",
                        ["Foundry:ImageModelDeployment"] = "test-image-deployment",
                        ["Speech:Endpoint"] = "https://example.speech.azure.com/",
                        ["Speech:ApiKey"] = "test-speech-key",
                        ["ContentUnderstanding:Endpoint"] = "https://example.services.ai.azure.com/",
                        ["ContentUnderstanding:ApiKey"] = "test-content-understanding-key"
                    });
                });
            });

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
