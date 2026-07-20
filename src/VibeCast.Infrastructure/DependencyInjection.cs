using System.ClientModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using VibeCast.Application.Abstractions.Jobs;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Episodes;
using VibeCast.Application.Media;
using VibeCast.Application.Validation;
using VibeCast.Infrastructure.AI;
using VibeCast.Infrastructure.Data;
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

        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            FoundryOptions options = serviceProvider
                .GetRequiredService<IOptions<FoundryOptions>>()
                .Value;

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Foundry:ApiKey is required for chat client registration.");
            }

            OpenAIClientOptions clientOptions = new()
            {
                Endpoint = new Uri(options.ProjectEndpoint)
            };

            ChatClient chatClient = new(
                options.ChatModelDeployment,
                new ApiKeyCredential(options.ApiKey),
                clientOptions);

            return chatClient.AsIChatClient();
        });

        services.AddScoped<
            IEpisodeConceptGenerator,
            FoundryEpisodeConceptGenerator>();

        return services;
    }
}
