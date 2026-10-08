using System.Security.Claims;
using Azure.Core;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using VibeCast.Application.Media;
using VibeCast.Infrastructure;
using VibeCast.Infrastructure.Authentication;
using VibeCast.Infrastructure.Data;
using VibeCast.ServiceDefaults;
using VibeCast.Web;
using VibeCast.Web.Components;
using VibeCast.Web.Security;
using VibeCast.Web.Telemetry;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

if (builder.Environment.IsDevelopment())
{
    builder.AddKeyedAzureBlobContainerClient("media");
    builder.AddKeyedAzureBlobContainerClient("data-protection");
}

TokenCredential azureCredential = AzureCredentialFactory.Create(
        builder.Configuration,
        builder.Environment);

builder.Services.AddSingleton(azureCredential);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.AddAuthorizationBuilder();
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 10;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<VibeCastDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/access-denied";
    if (builder.Environment.IsProduction())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});
builder.Services.AddAntiforgery(options =>
{
    if (builder.Environment.IsProduction())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

builder.Services.AddVibeCastInfrastructure(
    builder.Configuration,
    builder.Environment,
    azureCredential);

builder.AddVibeCastDataProtection(azureCredential);

var app = builder.Build();

await app.EnsureDevelopmentDataProtectionBlobAsync();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if(app.Environment.IsDevelopment())
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    IDbContextFactory<VibeCastDbContext> factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<VibeCastDbContext>>();

    await using VibeCastDbContext db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// Container Apps probes call the container over HTTP, without forwarded headers.
// Serve their status directly; ingress still enforces HTTPS for public access.
app.UseWhen(context => context.Request.Path != "/health" && context.Request.Path != "/alive",
    branch => branch.UseHttpsRedirection());
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapGet("/media/{mediaAssetId:guid}/content",
    async (
        Guid mediaAssetId,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IMediaAssetService mediaAssetService,
        CancellationToken cancellationToken) =>
    {
        string? ownerId =
            user.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Results.Unauthorized();
        }

        MediaContent? media = await mediaAssetService.OpenMediaAsync(
            mediaAssetId,
            ownerId,
            cancellationToken);

        if (media is null)
        {
            return Results.NotFound();
        }

        httpContext.Response.Headers.CacheControl =
            "private, no-store";

        return Results.Stream(
            media.Content,
            contentType: media.ContentType,
            enableRangeProcessing: true);
    }).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
