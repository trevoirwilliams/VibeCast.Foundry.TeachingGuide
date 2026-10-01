using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VibeCast.Infrastructure.Data;

public sealed class VibeCastDbContextFactory : IDesignTimeDbContextFactory<VibeCastDbContext>
{
    public VibeCastDbContext CreateDbContext(string[] args)
    {
        string connectionString = args.FirstOrDefault()
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__vibecast")
            ?? "Host=localhost;Database=vibecast;Username=postgres";

        DbContextOptions<VibeCastDbContext> options =
            new DbContextOptionsBuilder<VibeCastDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        return new VibeCastDbContext(options);
    }
}
