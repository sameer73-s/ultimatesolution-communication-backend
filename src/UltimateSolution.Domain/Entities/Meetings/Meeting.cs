using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Meetings;

public sealed class Meeting
{
    private Meeting()
    {
    }

    private Meeting(Guid id, string title, string? agenda, Guid organizerUserId, DateTimeOffset scheduledStartUtc, DateTimeOffset scheduledEndUtc)
    {
        Id = id;
        Title = title;
        Agenda = agenda;
        OrganizerUserId = organizerUserId;
        ScheduledStartUtc = scheduledStartUtc;
        ScheduledEndUtc = scheduledEndUtc;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Agenda { get; private set; }
    public Guid OrganizerUserId { get; private set; }
    public DateTimeOffset ScheduledStartUtc { get; private set; }
    public DateTimeOffset ScheduledEndUtc { get; private set; }
    public MeetingStatus Status { get; private set; } = MeetingStatus.Scheduled;
    public string? MediaSessionReference { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public ICollection<MeetingParticipant> Participants { get; } = new List<MeetingParticipant>();
    public ICollection<MeetingRecording> Recordings { get; } = new List<MeetingRecording>();
    public ICollection<TranscriptionJob> TranscriptionJobs { get; } = new List<TranscriptionJob>();
    public ICollection<MeetingSummary> Summaries { get; } = new List<MeetingSummary>();
    public ICollection<ActionItem> ActionItems { get; } = new List<ActionItem>();

    public static Meeting Schedule(string title, string? agenda, Guid organizerUserId, DateTimeOffset scheduledStartUtc, DateTimeOffset scheduledEndUtc)
    {
        ValidateSchedule(title, agenda, organizerUserId, scheduledStartUtc, scheduledEndUtc);
        return new Meeting(Guid.NewGuid(), title.Trim(), NormalizeAgenda(agenda), organizerUserId, scheduledStartUtc, scheduledEndUtc);
    }

    public void Update(string title, string? agenda, DateTimeOffset scheduledStartUtc, DateTimeOffset scheduledEndUtc)
    {
        EnsureScheduled();
        ValidateSchedule(title, agenda, OrganizerUserId, scheduledStartUtc, scheduledEndUtc);
        Title = title.Trim();
        Agenda = NormalizeAgenda(agenda);
        ScheduledStartUtc = scheduledStartUtc;
        ScheduledEndUtc = scheduledEndUtc;
    }

    public void Start(string mediaSessionReference, DateTimeOffset startedAtUtc)
    {
        EnsureScheduled();
        if (string.IsNullOrWhiteSpace(mediaSessionReference))
        {
            throw new DomainValidationException("A media session reference is required to start the meeting.");
        }

        Status = MeetingStatus.Active;
        MediaSessionReference = mediaSessionReference;
        StartedAtUtc = startedAtUtc;
    }

    public void End(DateTimeOffset endedAtUtc)
    {
        if (Status != MeetingStatus.Active)
        {
            throw new DomainValidationException("Only an active meeting can be ended.");
        }

        Status = MeetingStatus.Completed;
        EndedAtUtc = endedAtUtc;
    }

    public void AddParticipant(Guid userId, MeetingParticipantRole role, DateTimeOffset invitedAtUtc)
    {
        EnsureScheduled();
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException("A participant user identifier is required.");
        }

        if (Participants.Any(participant => participant.UserId == userId))
        {
            throw new DomainValidationException("The user is already a meeting participant.");
        }

        Participants.Add(new MeetingParticipant(Id, userId, role, invitedAtUtc));
    }

    public void RemoveParticipant(Guid userId)
    {
        EnsureScheduled();
        var participant = Participants.SingleOrDefault(candidate => candidate.UserId == userId)
            ?? throw new DomainNotFoundException("The meeting participant was not found.");
        if (participant.Role == MeetingParticipantRole.Organizer)
        {
            throw new DomainValidationException("The meeting organizer cannot be removed.");
        }

        Participants.Remove(participant);
    }

    private void EnsureScheduled()
    {
        if (Status != MeetingStatus.Scheduled)
        {
            throw new DomainValidationException("The meeting can only be changed while scheduled.");
        }
    }

    private static void ValidateSchedule(string title, string? agenda, Guid organizerUserId, DateTimeOffset scheduledStartUtc, DateTimeOffset scheduledEndUtc)
    {
        if (organizerUserId == Guid.Empty)
        {
            throw new DomainValidationException("A meeting organizer is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Meeting title is required.");
        }

        if (title.Trim().Length > 180)
        {
            throw new DomainValidationException("Meeting title cannot exceed 180 characters.");
        }

        if (agenda?.Length > 4000)
        {
            throw new DomainValidationException("Meeting agenda cannot exceed 4000 characters.");
        }

        if (scheduledEndUtc <= scheduledStartUtc)
        {
            throw new DomainValidationException("Meeting end time must be after start time.");
        }
    }

    private static string? NormalizeAgenda(string? agenda) => string.IsNullOrWhiteSpace(agenda) ? null : agenda.Trim();
}
