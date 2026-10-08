using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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
        settings["AzureIdentity__ManagedIdentityClientId"] = "00000000-0000-0000-0000-000000000001";
        settings["MediaStorage__ServiceUri"] = "https://example.blob.core.windows.net/";
        settings["DataProtection__BlobUri"] = "https://example.blob.core.windows.net/keys/keys.xml";
        settings["DataProtection__KeyVaultKeyIdentifier"] = "https://example.vault.azure.net/keys/test";
        settings["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";

        foreach (var pair in settings)
        {
            _previousEnvironment[pair.Key] =
                Environment.GetEnvironmentVariable(pair.Key);
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }
    }

    [TestMethod]
    public async Task ProductionForwardedHttps_LoginRendersWithoutRedirectLoop()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                // Keep remote key-ring calls out of this deterministic middleware test.
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                    services.Configure<KeyManagementOptions>(options =>
                    {
                        options.XmlRepository = new MemoryKeyRepository();
                        options.XmlEncryptor = null;
                    });
                });
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Login");
        request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await client.SendAsync(request);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "Sign in");
        Assert.IsNull(response.Headers.Location);
        Assert.IsTrue(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.IsTrue(cookies!.Any(cookie => cookie.Contains("secure", StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class MemoryKeyRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];
        public IReadOnlyCollection<XElement> GetAllElements() => _elements.ToArray();
        public void StoreElement(XElement element, string friendlyName) => _elements.Add(new XElement(element));
    }

    [TestMethod]
    public async Task HttpProbes_ReturnStatusWithoutHttpsRedirect()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false
        });
        foreach (string path in new[] { "/health", "/alive" })
        {
            using var response = await client.GetAsync(path);
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Healthy", await response.Content.ReadAsStringAsync());
            Assert.IsNull(response.Headers.Location);
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
