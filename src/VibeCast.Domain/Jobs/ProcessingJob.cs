using VibeCast.Domain.Common;

namespace VibeCast.Domain.Jobs;

public enum ProcessingJobStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public sealed class ProcessingJob : Entity
{
    private ProcessingJob() { }

    private ProcessingJob(string ownerId, string jobType, string? subjectReference)
    {
        OwnerId = ownerId;
        JobType = jobType;
        SubjectReference = subjectReference;
    }

    public string OwnerId { get; private set; } = string.Empty;
    public string JobType { get; private set; } = string.Empty;
    public string? SubjectReference { get; private set; }
    public ProcessingJobStatus Status { get; private set; } = ProcessingJobStatus.Queued;
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static ProcessingJob Queue(string ownerId, string jobType, string? subjectReference = null) =>
        new(ownerId, jobType, subjectReference);

    public void Start()
    {
        Status = ProcessingJobStatus.Running;
        StartedAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }

    public void Complete()
    {
        Status = ProcessingJobStatus.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        ErrorMessage = null;
        MarkUpdated();
    }

    public void Fail(string errorMessage)
    {
        Status = ProcessingJobStatus.Failed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
        MarkUpdated();
    }
}
