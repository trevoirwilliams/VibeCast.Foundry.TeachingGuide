using System.ClientModel;
using System.ClientModel.Primitives;
using System.Threading.RateLimiting;
using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.AI.OpenAI;
using Azure.AI.Speech.Transcription;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.KnowledgeBases;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VibeCast.Application.Abstractions.Jobs;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Episodes;
using VibeCast.Application.Knowledge;
using VibeCast.Application.Media;
using VibeCast.Application.Validation;
using VibeCast.Infrastructure.AI;
using VibeCast.Infrastructure.Data;
using VibeCast.Infrastructure.Episodes;
using VibeCast.Infrastructure.Jobs;
using VibeCast.Infrastructure.Media;
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

        services.AddOptions<KnowledgeStorageOptions>()
            .Bind(configuration.GetSection(KnowledgeStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.ServiceUri,
                        UriKind.Absolute,
                        out Uri? uri) &&
                    uri.Scheme == Uri.UriSchemeHttps,
                "KnowledgeStorage:ServiceUri must be an absolute HTTPS URI.")
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.SearchEndpoint,
                        UriKind.Absolute,
                        out Uri? uri) &&
                    uri.Scheme ==
                        Uri.UriSchemeHttps,
                "KnowledgeStorage:SearchEndpoint must be an absolute HTTPS URI.")
            .ValidateOnStart();


        services.AddOptions<BackgroundJobsOptions>()
            .Bind(configuration.GetSection(BackgroundJobsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SpeechOptions>()
            .Bind(configuration.GetSection(SpeechOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ContentUnderstandingOptions>()
            .Bind(configuration.GetSection(ContentUnderstandingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();


        services.AddSingleton<IBlobStorage, LocalBlobStorage>();
        services.AddSingleton<BlobServiceClient>(serviceProvider =>
        {
            KnowledgeStorageOptions options = serviceProvider.GetRequiredService<IOptions<KnowledgeStorageOptions>>()
                    .Value;

            return new BlobServiceClient(
                new Uri(
                    options.ServiceUri,
                    UriKind.Absolute),
                new DefaultAzureCredential());
        });

        services.AddSingleton<RateLimiter>(serviceProvider =>
        {
            FoundryOptions options = serviceProvider.GetRequiredService<IOptions<FoundryOptions>>().Value;

            return new ConcurrencyLimiter(
                new ConcurrencyLimiterOptions
                {
                    PermitLimit = options.MaxConcurrentChatRequests,
                    QueueLimit = options.ChatQueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });

        services.AddSingleton<BlobContainerClient>(serviceProvider =>
            {
                KnowledgeStorageOptions options = serviceProvider.GetRequiredService<IOptions<KnowledgeStorageOptions>>()
                        .Value;

                BlobServiceClient blobServiceClient = serviceProvider.GetRequiredService<BlobServiceClient>();

                return blobServiceClient.GetBlobContainerClient(options.ContainerName);
            });

        services.AddSingleton<KnowledgeBaseRetrievalClient>(
            serviceProvider =>
            {
                KnowledgeStorageOptions options = serviceProvider.GetRequiredService<IOptions<KnowledgeStorageOptions>>().Value;

                SearchClientOptions clientOptions = new(SearchClientOptions.ServiceVersion.V2026_04_01);

                return new KnowledgeBaseRetrievalClient(
                    new Uri(options.SearchEndpoint, UriKind.Absolute),
                    options.KnowledgeBaseName,
                    new DefaultAzureCredential(),
                    clientOptions);
            });

        services.AddSingleton<IKnowledgeSourceStorage, AzureBlobKnowledgeSourceStorage>();

        services.AddSingleton<IBackgroundJobQueue, ChannelBackgroundJobQueue>();
        services.AddHostedService<BackgroundJobWorker>();

        services.AddSingleton<IValidator<CreateEpisodeRequest>, EpisodeDraftValidator>();
        services.AddSingleton<IValidator<SupportingSourceAssessmentValidationRequest>, SupportingSourceAssessmentValidator>();
        services.AddSingleton<MediaUploadValidator>();
        services.AddSingleton<IValidator<MediaUploadRequest>>(sp =>
            sp.GetRequiredService<MediaUploadValidator>());
        services.AddSingleton<ArtworkAnalysisValidator>();
        services.AddSingleton<IValidator<ArtworkAnalysis>>(
            serviceProvider => serviceProvider.GetRequiredService<ArtworkAnalysisValidator>());

        services.AddSingleton<IValidator<EpisodePlan>, EpisodePlanValidator>();
        services.AddSingleton<IValidator<EpisodeFormatGuidanceValidationRequest>, EpisodeFormatGuidanceValidator>();

        services.AddScoped<IEpisodeService, EfEpisodeService>();
        services.AddScoped<IEpisodeFormatPolicyProvider, EfEpisodeFormatPolicyProvider>();
        services.AddScoped<IMediaAssetService, EfMediaAssetService>();

        IServiceCollection serviceCollection = services.AddSingleton(serviceProvider =>
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

            RateLimiter rateLimiter = serviceProvider.GetRequiredService<RateLimiter>();

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

            IChatClient resilientProviderClient = new ChatResilienceClient(
                providerClient,
                TimeSpan.FromSeconds(options.ChatTimeoutSeconds),
                rateLimiter);

            return new ChatClientBuilder(monitoredProviderClient)
            .UseFunctionInvocation(
                loggerFactory,
                functionClient =>
                {
                    functionClient.MaximumIterationsPerRequest = 4;
                    functionClient.MaximumConsecutiveErrorsPerRequest = 0;
                    functionClient.AllowConcurrentInvocation = false;
                })
            .UseOpenTelemetry(
                loggerFactory: loggerFactory,
                sourceName: VibeCastAiTelemetry.SourceName,
                configure: telemetry =>
                {
                    telemetry.EnableSensitiveData = false;
                })
            .Build(serviceProvider);
        });

        #pragma warning disable MEAI001
        services.AddSingleton<IImageGenerator>(serviceProvider =>
        {
            FoundryOptions options = serviceProvider
                .GetRequiredService<IOptions<FoundryOptions>>()
                .Value;

            AzureOpenAIClient azureOpenAIClient =
                serviceProvider
                    .GetRequiredService<AzureOpenAIClient>();

            return azureOpenAIClient
                .GetImageClient(options.ImageModelDeployment)
                .AsIImageGenerator();
        });
#pragma warning restore MEAI001

        services.AddSingleton<TranscriptionClient>(
        serviceProvider =>
        {
            SpeechOptions options = serviceProvider
                .GetRequiredService<IOptions<SpeechOptions>>()
                .Value;

            return new TranscriptionClient(
                new Uri(options.Endpoint, UriKind.Absolute),
                new ApiKeyCredential(options.ApiKey));
        });

        services.AddSingleton<ContentUnderstandingClient>(
        serviceProvider => {
            ContentUnderstandingOptions options = serviceProvider
                .GetRequiredService<IOptions<ContentUnderstandingOptions>>()
                .Value;

            return new ContentUnderstandingClient(
                new Uri(
                    options.Endpoint,
                    UriKind.Absolute),
                new AzureKeyCredential(
                    options.ApiKey));
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

        services.AddScoped<IArtworkAnalysisService, FoundryArtworkAnalysisService>();
        services.AddScoped<IEpisodeArtworkGenerationService, FoundryEpisodeArtworkGenerationService>();
        services.AddScoped<IEpisodeTranscriptionService, FoundryEpisodeTranscriptionService>();
        services.AddScoped<IEpisodeResourceAnalysisService, FoundryEpisodeResourceAnalysisService>();
        services.AddScoped<IGroundedBlogGenerationService, FoundryGroundedBlogGenerationService>();
        return services;
    }
}
