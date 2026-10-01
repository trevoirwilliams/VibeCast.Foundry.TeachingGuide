var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
        .WithDataVolume();
var vibecastDatabase = postgres.AddDatabase("vibecast");

var storage = builder.AddAzureStorage("storage")
        .RunAsEmulator(emulator => {
            emulator.WithDataVolume()
                .WithBlobPort(10000)
                .WithQueuePort(10001)
                .WithTablePort(10002);
        });
var mediaStorage = storage.AddBlobContainer(
        "media",
        "vibecast-media");

var dataProtectionStorage = storage.AddBlobContainer(
        "data-protection",
        "vibecast-dataprotection");

var foundryProjectEndpoint = builder.AddParameter("foundry-project-endpoint");
var foundryChatModelDeployment = builder.AddParameter("foundry-chat-model-deployment");
var foundryImageModelDeployment = builder.AddParameter("foundry-image-model-deployment");
var foundryApiKey = builder.AddParameter("foundry-api-key", secret: true);
var speechEndpoint = builder.AddParameter("speech-endpoint");
var speechApiKey = builder.AddParameter("speech-api-key", secret: true);
var contentUnderstandingEndpoint = builder.AddParameter("content-understanding-endpoint");
var contentUnderstandingApiKey = builder.AddParameter("content-understanding-api-key",
        secret: true);
var knowledgeStorageServiceUri = builder.AddParameter("knowledge-storage-service-uri");
var knowledgeSearchEndpoint = builder.AddParameter("knowledge-search-endpoint");

builder.AddDockerfile("vibecast-web", "..")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Foundry__ProjectEndpoint", foundryProjectEndpoint)
    .WithEnvironment("Foundry__ChatModelDeployment", foundryChatModelDeployment)
    .WithEnvironment("Foundry__ImageModelDeployment", foundryImageModelDeployment)
    .WithEnvironment("Foundry__ApiKey", foundryApiKey)
    .WithEnvironment("Speech__Endpoint", speechEndpoint)
    .WithEnvironment("Speech__ApiKey", speechApiKey)
    .WithEnvironment("ContentUnderstanding__Endpoint", contentUnderstandingEndpoint)
    .WithEnvironment("ContentUnderstanding__ApiKey", contentUnderstandingApiKey)
    .WithEnvironment("KnowledgeStorage__ServiceUri", knowledgeStorageServiceUri)
    .WithEnvironment("KnowledgeStorage__SearchEndpoint", knowledgeSearchEndpoint)
    .WithReference(vibecastDatabase)
    .WithReference(mediaStorage)
    .WithReference(dataProtectionStorage)
    .WaitFor(vibecastDatabase)
    .WaitFor(storage);

builder.Build().Run();
