using VibeCast.Domain.Common;

namespace VibeCast.Domain.Episodes;

public sealed class Episode : Entity
{
    private Episode() { }

    private Episode(string title, string? description, string ownerId)
    {
        SetTitle(title);
        Description = description?.Trim();
        OwnerId = string.IsNullOrWhiteSpace(ownerId)
            ? throw new ArgumentException("An owner is required.", nameof(ownerId))
            : ownerId;
    }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public EpisodeStatus Status { get; private set; } = EpisodeStatus.Draft;
    public DateTimeOffset? ScheduledForUtc { get; private set; }

    public static Episode Create(string title, string? description, string ownerId) =>
        new(title, description, ownerId);

    public void Rename(string title)
    {
        SetTitle(title);
        MarkUpdated();
    }

    public void Schedule(DateTimeOffset scheduledForUtc)
    {
        ScheduledForUtc = scheduledForUtc;
        Status = EpisodeStatus.ReadyForReview;
        MarkUpdated();
    }

    private void SetTitle(string title)
    {
        var value = title?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 160)
        {
            throw new ArgumentException("Episode title must contain 1 to 160 characters.", nameof(title));
        }

        Title = value;
    }
}
