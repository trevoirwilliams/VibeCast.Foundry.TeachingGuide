using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Threading.RateLimiting;
using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.AI.OpenAI;
using Azure.AI.Speech.Transcription;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.KnowledgeBases;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.PostgreSQL.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
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
    public static IServiceCollection AddVibeCastInfrastructure(this IServiceCollection services,
      IConfiguration configuration,
      IWebHostEnvironment environment,
      TokenCredential azureCredential)
    {
        var connectionString = configuration.GetConnectionString("VibeCast")
            ?? throw new InvalidOperationException(
        "The VibeCast PostgreSQL connection string is not configured.");

        if (environment.IsProduction())
        {
            services.AddSingleton<NpgsqlDataSource>(_ =>
            {
                NpgsqlDataSourceBuilder dataSourceBuilder = new(connectionString);
                dataSourceBuilder.UseEntraAuthentication(azureCredential);
                return dataSourceBuilder.Build();
            });

            services.AddDbContextFactory<VibeCastDbContext>(
                (serviceProvider, options) =>
                {
                    NpgsqlDataSource dataSource = serviceProvider.GetRequiredService<
                            NpgsqlDataSource>();

                    options.UseNpgsql(dataSource);
                });
        }
        else
        {
            services.AddDbContextFactory<VibeCastDbContext>(options =>
            {
                // PRACTICE S09-04: Configure the local PostgreSQL provider.
                // Lesson: Move Relational Data to PostgreSQL for Production.
                // 1. Use the supplied connectionString to configure options for PostgreSQL.
                // 2. This callback configures the options builder; it does not return a DbContext.
                //    AppHost supplies the runtime connection string. Do not restore a SQLite fallback.
                // The production Entra registration above and design-time factory are supplied.
                // Check: start AppHost after S09-05/06, sign in, and retain data after restarting.
                // Optional API hint: docs/practice/README.md#s09-04-postgresql
                throw new NotImplementedException("S09-04: configure PostgreSQL options.");
            });
        }

        services.AddOptions<AzureIdentityOptions>()
        .Bind(configuration.GetSection(AzureIdentityOptions.SectionName))
        .Validate(options =>
                !environment.IsProduction() || Guid.TryParse(
                    options.ManagedIdentityClientId,
                    out _),
            "AzureIdentity:ManagedIdentityClientId must be a valid GUID in Production.")
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

        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName))
            .Validate(options =>
                    !environment.IsDevelopment() || !string.IsNullOrWhiteSpace(
                        options.ApiKey),
                "Foundry:ApiKey is required in Development.")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SpeechOptions>()
            .Bind(configuration.GetSection(SpeechOptions.SectionName))
            .Validate(options =>
                !environment.IsDevelopment() || !string.IsNullOrWhiteSpace(
                    options.ApiKey),
            "Speech:ApiKey is required in Development.")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ContentUnderstandingOptions>()
            .Bind(configuration.GetSection(ContentUnderstandingOptions.SectionName))
            .Validate(options =>
                    !environment.IsDevelopment() || !string.IsNullOrWhiteSpace(
                        options.ApiKey),
                "ContentUnderstanding:ApiKey is required in Development.")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (environment.IsProduction())
        {
            services.AddOptions<MediaStorageOptions>()
                .Bind(configuration.GetSection(MediaStorageOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(options => 
                        Uri.TryCreate(options.ServiceUri,UriKind.Absolute,
                            out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps,
                    "MediaStorage:ServiceUri must be an absolute HTTPS URI.")
                .ValidateOnStart();
        }

        if (environment.IsProduction())
        {
            services.AddKeyedSingleton<BlobContainerClient>("media",
                (serviceProvider, _) =>
                {
                    MediaStorageOptions options = serviceProvider.GetRequiredService<
                                IOptions<MediaStorageOptions>>().Value;
                    Uri serviceUri = new(options.ServiceUri, UriKind.Absolute);
                    Uri containerUri = new(serviceUri, options.ContainerName);

                    return new BlobContainerClient(
                        containerUri,
                        azureCredential);
                });
        }

        services.AddSingleton<IBlobStorage>(provider =>
        {
            // PRACTICE S09-06: Resolve the media storage client.
            // Lesson: Normalize Container Configurations and Run Locally.
            // 1. Resolve BlobContainerClient using the "media" service key.
            //    The unkeyed container is for knowledge sources, not uploaded media.
            // 2. Resolve ILogger<AzureBlobStorage> from the same provider.
            // 3. Return a new AzureBlobStorage with those two dependencies, satisfying IBlobStorage.
            // Check: an upload appears under owners/{ownerKey}/media in vibecast-media.
            // Optional API hint: docs/practice/README.md#s09-06-media-client
            throw new NotImplementedException("S09-06: register the keyed media storage implementation.");
        });


        services.AddSingleton<BlobServiceClient>(serviceProvider =>
        {
            KnowledgeStorageOptions options = serviceProvider.GetRequiredService<IOptions<KnowledgeStorageOptions>>()
                    .Value;

            return new BlobServiceClient(
                new Uri(
                    options.ServiceUri,
                    UriKind.Absolute),
                azureCredential);
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
                    azureCredential,
                    clientOptions);
            });

        services.AddSingleton<IKnowledgeSourceStorage, AzureBlobKnowledgeSourceStorage>();
        
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

        IServiceCollection serviceCollection = services.AddSingleton(
        serviceProvider =>
        {
            FoundryOptions options = serviceProvider
                .GetRequiredService<IOptions<FoundryOptions>>()
                .Value;

            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            AzureOpenAIClientOptions clientOptions = new()
            {
                RetryPolicy = new ClientRetryPolicy(
                    maxRetries: options.MaxRetries,
                    enableLogging: true,
                    loggerFactory: loggerFactory)
            };

            Uri endpoint = new Uri(
                    options.ProjectEndpoint,
                    UriKind.Absolute);

            if (environment.IsProduction())
            {
                return new AzureOpenAIClient(
                    endpoint,
                    azureCredential,
                    clientOptions);
            }

            return new AzureOpenAIClient(
                endpoint,
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

            IChatClient resilientProviderClient = new ChatResilienceClient(
                providerClient,
                TimeSpan.FromSeconds(options.ChatTimeoutSeconds),
                rateLimiter);

            IChatClient monitoredProviderClient = new ChatResponseLoggingClient(
                resilientProviderClient,
                logger);

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

            Uri endpoint =
            new(
                options.Endpoint,
                UriKind.Absolute);

            if (environment.IsProduction())
            {
                return new TranscriptionClient(
                    endpoint,
                    azureCredential);
            }

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException(
                    "TranscriptionClient:ApiKey is required outside Production.");
            }

            return new TranscriptionClient(
                endpoint,
                new ApiKeyCredential(options.ApiKey));
        });

        services.AddSingleton<ContentUnderstandingClient>(
        serviceProvider => {
            ContentUnderstandingOptions options = serviceProvider
                .GetRequiredService<IOptions<ContentUnderstandingOptions>>()
                .Value;

            Uri endpoint =
            new(
                options.Endpoint,
                UriKind.Absolute);

            if (environment.IsProduction())
            {
                return new ContentUnderstandingClient(
                    endpoint,
                    azureCredential);
            }

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException(
                    "ContentUnderstanding:ApiKey is required outside Production.");
            }

            return new ContentUnderstandingClient(
                endpoint,
                new AzureKeyCredential(options.ApiKey));
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
