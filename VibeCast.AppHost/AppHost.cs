var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.VibeCast_Web>("vibecast-web");

builder.Build().Run();
