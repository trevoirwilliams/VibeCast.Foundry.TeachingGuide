namespace VibeCast.Application.Abstractions.Jobs;

public interface IBackgroundJobQueue
{
    ValueTask QueueAsync(BackgroundJob job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<BackgroundJob> ReadAllAsync(CancellationToken cancellationToken = default);
}

public sealed record BackgroundJob(
    Guid Id,
    string Name,
    Func<IServiceProvider, CancellationToken, Task> ExecuteAsync)
{
    public static BackgroundJob Create(
        string name,
        Func<IServiceProvider, CancellationToken, Task> executeAsync) =>
        new(Guid.NewGuid(), name, executeAsync);
}
