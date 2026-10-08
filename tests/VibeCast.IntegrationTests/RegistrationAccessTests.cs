using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Web.Pages.Account;
using VibeCast.Web.Security;

namespace VibeCast.IntegrationTests;

[TestClass]
public sealed class RegistrationAccessTests
{
    private static IConfiguration Settings(bool enabled, string? allowedEmail = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Registration:Enabled"] = enabled.ToString(),
            ["Registration:AllowedEmail"] = allowedEmail
        }).Build();

    private sealed class Host(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "VibeCast";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [TestMethod]
    public async Task ProductionRegistration_FailsClosedOnGetAndPostWithoutAllowedEmail()
    {
        var model = new RegisterModel(null!, null!, Settings(true), new Host("Production"));
        Assert.IsInstanceOfType<NotFoundResult>(model.OnGet());
        Assert.IsInstanceOfType<NotFoundResult>(await model.OnPostAsync());
    }

    [TestMethod]
    public async Task ProductionRegistration_RejectsOtherEmailBeforeCallingIdentityStore()
    {
        var model = new RegisterModel(null!, null!, Settings(true, "demo@example.test"), new Host("Production"))
        {
            Input = new RegisterModel.InputModel { Email = "other@example.test" }
        };
        Assert.IsInstanceOfType<NotFoundResult>(await model.OnPostAsync());
    }

    [TestMethod]
    public void TemporaryOnboarding_AcceptsOnlyDesignatedEmailAndClosesWhenDisabled()
    {
        var host = new Host("Production");
        Assert.IsTrue(RegistrationAccess.AllowsEmail(Settings(true, "demo@example.test"), host, "DEMO@example.test"));
        Assert.IsFalse(RegistrationAccess.AllowsEmail(Settings(false, "demo@example.test"), host, "demo@example.test"));
        Assert.IsFalse(RegistrationAccess.AllowsEmail(Settings(true, "demo@example.test"), host, "other@example.test"));
        Assert.IsTrue(RegistrationAccess.AllowsEmail(Settings(true), new Host("Development"), "local@example.test"));
    }
}
