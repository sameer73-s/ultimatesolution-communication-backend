using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Meetings;

public sealed class MeetingRecording
{
    private MeetingRecording()
    {
    }

    public MeetingRecording(Guid meetingId, Guid requestedByUserId, string mediaRecordingReference, DateTimeOffset startedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(mediaRecordingReference))
        {
            throw new DomainValidationException("A media recording reference is required.");
        }

        Id = Guid.NewGuid();
        MeetingId = meetingId;
        RequestedByUserId = requestedByUserId;
        MediaRecordingReference = mediaRecordingReference;
        StartedAtUtc = startedAtUtc;
        Status = RecordingStatus.Recording;
    }

    public Guid Id { get; private set; }
    public Guid MeetingId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string MediaRecordingReference { get; private set; } = string.Empty;
    public RecordingStatus Status { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? StoppedAtUtc { get; private set; }
    public DateTimeOffset? AvailableAtUtc { get; private set; }

    public void Stop(RecordingStatus status, DateTimeOffset stoppedAtUtc, DateTimeOffset? availableAtUtc)
    {
        if (Status != RecordingStatus.Recording)
        {
            throw new DomainValidationException("Only an active recording can be stopped.");
        }

        Status = status;
        StoppedAtUtc = stoppedAtUtc;
        AvailableAtUtc = availableAtUtc;
    }
}
