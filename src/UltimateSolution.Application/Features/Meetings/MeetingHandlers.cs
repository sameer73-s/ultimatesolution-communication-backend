using Mediator;
using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Meetings;
using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Application.Features.Meetings;

public sealed class ScheduleMeetingCommandHandler(IMeetingRepository meetingRepository, IUserDirectory userDirectory, IUnitOfWork unitOfWork) : IRequestHandler<ScheduleMeetingCommand, MeetingDto>
{
    public async ValueTask<MeetingDto> Handle(ScheduleMeetingCommand request, CancellationToken cancellationToken)
    {
        var participantIds = request.ParticipantUserIds.Append(request.RequestingUserId).Distinct().ToArray();
        foreach (var participantId in participantIds)
        {
            if (!await userDirectory.ExistsAsync(participantId, cancellationToken)) throw new DomainValidationException("Every meeting participant must be an existing user.");
        }

        var meeting = Meeting.Schedule(request.Title, request.Agenda, request.RequestingUserId, request.ScheduledStartUtc, request.ScheduledEndUtc);
        var now = DateTimeOffset.UtcNow;
        foreach (var participantId in participantIds) meeting.AddParticipant(participantId, participantId == request.RequestingUserId ? MeetingParticipantRole.Organizer : MeetingParticipantRole.Attendee, now);
        meetingRepository.Add(meeting);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MeetingMapper.Map(meeting);
    }
}

public sealed class GetMeetingsQueryHandler(IMeetingRepository meetingRepository) : IRequestHandler<GetMeetingsQuery, IReadOnlyCollection<MeetingDto>>
{
    public async ValueTask<IReadOnlyCollection<MeetingDto>> Handle(GetMeetingsQuery request, CancellationToken cancellationToken) => (await meetingRepository.GetForUserAsync(request.RequestingUserId, cancellationToken)).Select(MeetingMapper.Map).ToArray();
}

public sealed class GetMeetingQueryHandler(IMeetingRepository meetingRepository) : IRequestHandler<GetMeetingQuery, MeetingDto>
{
    public async ValueTask<MeetingDto> Handle(GetMeetingQuery request, CancellationToken cancellationToken) => MeetingMapper.Map(await MeetingAuthorization.GetParticipantMeetingAsync(meetingRepository, request.RequestingUserId, request.MeetingId, cancellationToken));
}

public sealed class UpdateMeetingCommandHandler(IMeetingRepository meetingRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateMeetingCommand, MeetingDto>
{
    public async ValueTask<MeetingDto> Handle(UpdateMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        MeetingAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        meeting.Update(request.Title, request.Agenda, request.ScheduledStartUtc, request.ScheduledEndUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MeetingMapper.Map(meeting);
    }
}

public sealed class InviteMeetingParticipantCommandHandler(IMeetingRepository meetingRepository, IUserDirectory userDirectory, IUnitOfWork unitOfWork) : IRequestHandler<InviteMeetingParticipantCommand, MeetingDto>
{
    public async ValueTask<MeetingDto> Handle(InviteMeetingParticipantCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        MeetingAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        if (!await userDirectory.ExistsAsync(request.ParticipantUserId, cancellationToken)) throw new DomainValidationException("The meeting participant must be an existing user.");
        meeting.AddParticipant(request.ParticipantUserId, MeetingParticipantRole.Attendee, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MeetingMapper.Map(meeting);
    }
}

public sealed class RemoveMeetingParticipantCommandHandler(IMeetingRepository meetingRepository, IUnitOfWork unitOfWork) : IRequestHandler<RemoveMeetingParticipantCommand, MeetingDto>
{
    public async ValueTask<MeetingDto> Handle(RemoveMeetingParticipantCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        MeetingAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        meeting.RemoveParticipant(request.ParticipantUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MeetingMapper.Map(meeting);
    }
}

public sealed class StartMeetingCommandHandler(IMeetingRepository meetingRepository, IMeetingMediaService meetingMediaService, IUnitOfWork unitOfWork) : IRequestHandler<StartMeetingCommand, MeetingDto>
{
    public async ValueTask<MeetingDto> Handle(StartMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        MeetingAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        var session = MeetingMediaResult.Require(await meetingMediaService.StartMeetingAsync(
            new StartMeetingMediaRequest(meeting.Id, meeting.OrganizerUserId, meeting.ScheduledStartUtc, meeting.Participants.Select(participant => participant.UserId).ToArray()),
            cancellationToken));
        meeting.Start(session.MediaSessionReference, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MeetingMapper.Map(meeting);
    }
}

public sealed class EndMeetingCommandHandler(IMeetingRepository meetingRepository, IMeetingMediaService meetingMediaService, IUnitOfWork unitOfWork) : IRequestHandler<EndMeetingCommand, MeetingDto>
{
    public async ValueTask<MeetingDto> Handle(EndMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        MeetingAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        if (string.IsNullOrWhiteSpace(meeting.MediaSessionReference)) throw new DomainValidationException("The active meeting does not have a media session.");
        MeetingMediaResult.Require(await meetingMediaService.EndMeetingAsync(
            new EndMeetingMediaRequest(meeting.Id, request.RequestingUserId, meeting.MediaSessionReference),
            cancellationToken));
        meeting.End(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MeetingMapper.Map(meeting);
    }
}

public sealed class JoinMeetingCommandHandler(IMeetingRepository meetingRepository, IMeetingMediaService meetingMediaService, IUnitOfWork unitOfWork) : IRequestHandler<JoinMeetingCommand, JoinMeetingResult>
{
    public async ValueTask<JoinMeetingResult> Handle(JoinMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetParticipantMeetingAsync(meetingRepository, request.RequestingUserId, request.MeetingId, cancellationToken);
        if (meeting.Status != MeetingStatus.Active || string.IsNullOrWhiteSpace(meeting.MediaSessionReference)) throw new DomainValidationException("Only an active meeting can be joined.");
        var participant = meeting.Participants.Single(participant => participant.UserId == request.RequestingUserId);
        var result = MeetingMediaResult.Require(await meetingMediaService.JoinParticipantAsync(
            new JoinMeetingParticipantRequest(meeting.Id, request.RequestingUserId, participant.Role, meeting.MediaSessionReference),
            cancellationToken));
        participant.MarkJoined(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}

public sealed class LeaveMeetingCommandHandler(IMeetingRepository meetingRepository, IMeetingMediaService meetingMediaService, IUnitOfWork unitOfWork) : IRequestHandler<LeaveMeetingCommand, Unit>
{
    public async ValueTask<Unit> Handle(LeaveMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetParticipantMeetingAsync(meetingRepository, request.RequestingUserId, request.MeetingId, cancellationToken);
        if (string.IsNullOrWhiteSpace(meeting.MediaSessionReference)) throw new DomainValidationException("The meeting does not have a media session.");
        MeetingMediaResult.Require(await meetingMediaService.LeaveParticipantAsync(
            new LeaveMeetingParticipantRequest(meeting.Id, request.RequestingUserId, meeting.MediaSessionReference),
            cancellationToken));
        meeting.Participants.Single(participant => participant.UserId == request.RequestingUserId).MarkLeft(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class StartMeetingRecordingCommandHandler(IMeetingRepository meetingRepository, IMeetingMediaService meetingMediaService, IUnitOfWork unitOfWork) : IRequestHandler<StartMeetingRecordingCommand, MeetingRecordingDto>
{
    public async ValueTask<MeetingRecordingDto> Handle(StartMeetingRecordingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        MeetingAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        if (meeting.Status != MeetingStatus.Active || string.IsNullOrWhiteSpace(meeting.MediaSessionReference)) throw new DomainValidationException("Only an active meeting can be recorded.");
        var result = MeetingMediaResult.Require(await meetingMediaService.StartRecordingAsync(
            new StartRecordingRequest(meeting.Id, request.RequestingUserId, meeting.MediaSessionReference),
            cancellationToken));
        var recording = new MeetingRecording(meeting.Id, request.RequestingUserId, result.MediaRecordingReference, DateTimeOffset.UtcNow);
        meeting.Recordings.Add(recording);
        meetingRepository.AddRecording(recording);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MeetingMapper.Map(recording);
    }
}

public sealed class StopMeetingRecordingCommandHandler(IMeetingRepository meetingRepository, IMeetingMediaService meetingMediaService, IUnitOfWork unitOfWork) : IRequestHandler<StopMeetingRecordingCommand, MeetingRecordingDto>
{
    public async ValueTask<MeetingRecordingDto> Handle(StopMeetingRecordingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await MeetingAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        MeetingAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        if (string.IsNullOrWhiteSpace(meeting.MediaSessionReference)) throw new DomainValidationException("The meeting does not have a media session.");
        var recording = meeting.Recordings.LastOrDefault(candidate => candidate.Status == RecordingStatus.Recording)
            ?? throw new DomainNotFoundException("No active meeting recording was found.");
        var result = MeetingMediaResult.Require(await meetingMediaService.StopRecordingAsync(
            new StopRecordingRequest(meeting.Id, request.RequestingUserId, meeting.MediaSessionReference, recording.MediaRecordingReference),
            cancellationToken));
        recording.Stop(result.Status, DateTimeOffset.UtcNow, result.AvailableAtUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MeetingMapper.Map(recording);
    }
}

public sealed class GetMeetingRecordingsQueryHandler(IMeetingRepository meetingRepository) : IRequestHandler<GetMeetingRecordingsQuery, IReadOnlyCollection<MeetingRecordingDto>>
{
    public async ValueTask<IReadOnlyCollection<MeetingRecordingDto>> Handle(GetMeetingRecordingsQuery request, CancellationToken cancellationToken) => (await MeetingAuthorization.GetParticipantMeetingAsync(meetingRepository, request.RequestingUserId, request.MeetingId, cancellationToken)).Recordings.Select(MeetingMapper.Map).ToArray();
}

internal static class MeetingMediaResult
{
    public static void Require(Result result)
    {
        if (!result.IsSuccess)
        {
            throw new DomainValidationException("The meeting media operation could not be completed.");
        }
    }

    public static T Require<T>(Result<T> result)
    {
        Require((Result)result);
        return result.Value ?? throw new DomainValidationException("The meeting media operation returned no result.");
    }
}

internal static class MeetingAuthorization
{
    public static async Task<Meeting> GetMeetingAsync(IMeetingRepository repository, Guid meetingId, CancellationToken cancellationToken) => await repository.GetByIdAsync(meetingId, cancellationToken) ?? throw new DomainNotFoundException("The meeting was not found.");
    public static async Task<Meeting> GetParticipantMeetingAsync(IMeetingRepository repository, Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await GetMeetingAsync(repository, meetingId, cancellationToken);
        if (meeting.Participants.All(participant => participant.UserId != userId)) throw new DomainForbiddenException("You are not a participant of this meeting.");
        return meeting;
    }
    public static void EnsureOrganizerOrManager(Meeting meeting, Guid userId, bool isManager)
    {
        if (!isManager && meeting.OrganizerUserId != userId) throw new DomainForbiddenException("Only the meeting organizer or a manager can perform this action.");
    }
}

internal static class MeetingMapper
{
    public static MeetingDto Map(Meeting meeting) => new(meeting.Id, meeting.Title, meeting.Agenda, meeting.OrganizerUserId, meeting.ScheduledStartUtc, meeting.ScheduledEndUtc, meeting.Status, meeting.MediaSessionReference, meeting.StartedAtUtc, meeting.EndedAtUtc, meeting.Participants.Select(participant => new MeetingParticipantDto(participant.UserId, participant.Role, participant.InvitedAtUtc, participant.JoinedAtUtc, participant.LeftAtUtc)).ToArray(), meeting.Recordings.Select(Map).ToArray());
    public static MeetingRecordingDto Map(MeetingRecording recording) => new(recording.Id, recording.MediaRecordingReference, recording.Status, recording.StartedAtUtc, recording.StoppedAtUtc, recording.AvailableAtUtc);
}
