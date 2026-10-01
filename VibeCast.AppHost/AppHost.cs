var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
        .WithDataVolume();
var vibecastDatabase = postgres.AddDatabase("vibecast");

var storage = builder.AddAzureStorage("storage")
        .RunAsEmulator(emulator => emulator.WithDataVolume());
var mediaStorage = storage.AddBlobContainer(
        "media",
        "vibecast-media");

var dataProtectionStorage = storage.AddBlobContainer(
        "data-protection",
        "vibecast-dataprotection");

builder.AddProject<Projects.VibeCast_Web>("vibecast-web")
     .WithReference(vibecastDatabase)
     .WithReference(mediaStorage)
     .WithReference(dataProtectionStorage)
     .WaitFor(vibecastDatabase)
     .WaitFor(storage);

builder.Build().Run();
