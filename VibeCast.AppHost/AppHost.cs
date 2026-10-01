var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
        .WithDataVolume();
var vibecastDatabase = postgres.AddDatabase("vibecast");

builder.AddProject<Projects.VibeCast_Web>("vibecast-web")
     .WithReference(vibecastDatabase)
     .WaitFor(vibecastDatabase);

builder.Build().Run();
