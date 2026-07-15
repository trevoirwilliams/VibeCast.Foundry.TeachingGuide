using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VibeCast.IntegrationTests;

[TestClass]
public sealed class HealthEndpointTests
{
    [TestMethod]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
