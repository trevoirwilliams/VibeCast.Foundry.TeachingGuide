using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Abstractions.Jobs;

namespace VibeCast.Infrastructure.Jobs;

public sealed class BackgroundJobWorker(
    IBackgroundJobQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                using var logScope = logger.BeginScope(new Dictionary<string, object>
                {
                    ["BackgroundJobId"] = job.Id,
                    ["BackgroundJobName"] = job.Name
                });

                logger.LogInformation("Starting background job {BackgroundJobName}", job.Name);
                await job.ExecuteAsync(scope.ServiceProvider, stoppingToken);
                logger.LogInformation("Completed background job {BackgroundJobName}", job.Name);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Background job {BackgroundJobName} failed", job.Name);
            }
        }
    }
}
