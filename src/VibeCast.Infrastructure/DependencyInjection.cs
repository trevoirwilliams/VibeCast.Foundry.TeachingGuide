using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VibeCast.Application.Abstractions.Jobs;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Episodes;
using VibeCast.Application.Media;
using VibeCast.Application.Validation;
using VibeCast.Infrastructure.AI;
using VibeCast.Infrastructure.Data;
using VibeCast.Infrastructure.Episodes;
using VibeCast.Infrastructure.Jobs;
using VibeCast.Infrastructure.Options;
using VibeCast.Infrastructure.Storage;

namespace VibeCast.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVibeCastInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("VibeCast")
            ?? "Data Source=.vibecast/vibecast.db";

        services.AddDbContextFactory<VibeCastDbContext>(options => options.UseSqlite(connectionString));

        services.AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(BlobStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<BackgroundJobsOptions>()
            .Bind(configuration.GetSection(BackgroundJobsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IBlobStorage, LocalBlobStorage>();
        services.AddSingleton<IBackgroundJobQueue, ChannelBackgroundJobQueue>();
        services.AddHostedService<BackgroundJobWorker>();

        services.AddSingleton<IValidator<CreateEpisodeRequest>, EpisodeDraftValidator>();
        services.AddSingleton<IValidator<MediaUploadRequest>, MediaUploadValidator>();
        services.AddSingleton<IValidator<EpisodePlan>, EpisodePlanValidator>();
        services.AddSingleton<IValidator<EpisodeFormatGuidanceValidationRequest>, EpisodeFormatGuidanceValidator>();

        services.AddScoped<IEpisodeService, EfEpisodeService>();
        services.AddScoped<IEpisodeFormatPolicyProvider, EfEpisodeFormatPolicyProvider>();

        services.AddSingleton<AzureOpenAIClient>(serviceProvider =>
        {
            FoundryOptions options = serviceProvider
                .GetRequiredService<IOptions<FoundryOptions>>()
                .Value;

            ILoggerFactory loggerFactory =
                serviceProvider
                    .GetRequiredService<ILoggerFactory>();

            AzureOpenAIClientOptions clientOptions = new()
            {
                RetryPolicy = new ClientRetryPolicy(
                    maxRetries: options.MaxRetries,
                    enableLogging: true,
                    loggerFactory: loggerFactory)
            };

            return new AzureOpenAIClient(
                new Uri(
                    options.ProjectEndpoint,
                    UriKind.Absolute),
                new ApiKeyCredential(options.ApiKey ?? string.Empty),
                clientOptions);
        });

        services.AddSingleton<IChatClient>(serviceProvider =>
        {
        FoundryOptions options = serviceProvider
            .GetRequiredService<IOptions<FoundryOptions>>()
            .Value;

        AzureOpenAIClient azureOpenAIClient =
            serviceProvider.GetRequiredService<AzureOpenAIClient>();

        ILogger<ChatResponseLoggingClient> logger =
            serviceProvider.GetRequiredService<
                ILogger<ChatResponseLoggingClient>>();

        ILoggerFactory loggerFactory =
           serviceProvider.GetRequiredService<
               ILoggerFactory>();

        IChatClient providerClient = azureOpenAIClient
           .GetChatClient(options.ChatModelDeployment)
           .AsIChatClient();

        IChatClient monitoredProviderClient = new ChatResponseLoggingClient(
            providerClient,
            logger);

            return new ChatClientBuilder(monitoredProviderClient)
            .UseFunctionInvocation(
                loggerFactory,
                functionClient =>
                {
                    functionClient.MaximumIterationsPerRequest = 4;

                    functionClient
                        .MaximumConsecutiveErrorsPerRequest = 0;

                    functionClient.AllowConcurrentInvocation = false;
                })
            .Build(serviceProvider);
        });

        services.AddScoped<
            IEpisodeConceptGenerator,
            FoundryEpisodeConceptGenerator>();

        services.AddKeyedScoped<
            IEpisodePlanningService,
            FoundryEpisodePlanningService>(
            EpisodePlanningServiceKeys.WithoutTools);

        services.AddKeyedScoped<
            IEpisodePlanningService,
            FoundryEpisodePlanningWithToolService>(
            EpisodePlanningServiceKeys.WithTools);

        return services;
    }
}
