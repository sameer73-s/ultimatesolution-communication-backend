using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Features.Meetings;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Infrastructure.ExternalServices.Meetings;

public sealed class JitsiMeetingMediaService(IOptions<JitsiMeetingMediaOptions> options) : IMeetingMediaService
{
    private readonly ConcurrentDictionary<string, MediaSessionState> _sessions = new();
    private readonly ConcurrentDictionary<string, byte> _recordings = new();
    private readonly JitsiMeetingMediaOptions _options = options.Value;

    public Task<Result<MeetingMediaSession>> StartMeetingAsync(
        StartMeetingMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(GetJoinUrlLifetimeMinutes());
            var reference = $"media-session-{Guid.NewGuid():N}";
            _sessions[reference] = new MediaSessionState(BuildRoomName(request.MeetingId), expiresAtUtc);
            return Task.FromResult(ResultFactory.Success(
                new MeetingMediaSession(reference, expiresAtUtc, "Active")));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(ResultFactory.Failure<MeetingMediaSession>("media_start_failed"));
        }
    }

    public Task<Result> EndMeetingAsync(
        EndMeetingMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_sessions.TryRemove(request.MediaSessionReference, out _))
            {
                return Task.FromResult(Result.Failure("media_session_not_found"));
            }

            return Task.FromResult(Result.Success());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(Result.Failure("media_end_failed"));
        }
    }

    public Task<Result<JoinMeetingResult>> JoinParticipantAsync(
        JoinMeetingParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var session = GetSession(request.MediaSessionReference);
            var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(GetJoinUrlLifetimeMinutes());
            var token = CreateToken(session.RoomName, request.UserId, expiresAtUtc);
            var joinUrl = $"{GetBaseUrl().TrimEnd('/')}/{Uri.EscapeDataString(session.RoomName)}?jwt={Uri.EscapeDataString(token)}";
            return Task.FromResult(ResultFactory.Success(
                new JoinMeetingResult(request.MediaSessionReference, joinUrl, expiresAtUtc)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(ResultFactory.Failure<JoinMeetingResult>("media_join_failed"));
        }
    }

    public Task<Result> LeaveParticipantAsync(
        LeaveMeetingParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = GetSession(request.MediaSessionReference);
            return Task.FromResult(Result.Success());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(Result.Failure("media_leave_failed"));
        }
    }

    public Task<Result<RecordingResult>> StartRecordingAsync(
        StartRecordingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = GetSession(request.MediaSessionReference);
            var reference = $"media-recording-{Guid.NewGuid():N}";
            _recordings[reference] = 0;
            return Task.FromResult(ResultFactory.Success(
                new RecordingResult(reference, RecordingStatus.Recording, null)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(ResultFactory.Failure<RecordingResult>("media_recording_start_failed"));
        }
    }

    public Task<Result<RecordingResult>> StopRecordingAsync(
        StopRecordingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = GetSession(request.MediaSessionReference);
            if (!_recordings.TryRemove(request.MediaRecordingReference, out _))
            {
                return Task.FromResult(ResultFactory.Failure<RecordingResult>("media_recording_not_found"));
            }

            return Task.FromResult(ResultFactory.Success(
                new RecordingResult(request.MediaRecordingReference, RecordingStatus.Processing, null)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(ResultFactory.Failure<RecordingResult>("media_recording_stop_failed"));
        }
    }

    private MediaSessionState GetSession(string reference) =>
        _sessions.TryGetValue(reference, out var session)
            ? session
            : throw new InvalidOperationException("The active media session was not found.");

    private string GetBaseUrl() => Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUrl)
        ? baseUrl.ToString().TrimEnd('/')
        : throw new InvalidOperationException("Meeting media BaseUrl must be an absolute URL.");

    private int GetJoinUrlLifetimeMinutes() => _options.JoinUrlLifetimeMinutes is > 0 and <= 60
        ? _options.JoinUrlLifetimeMinutes
        : throw new InvalidOperationException("Meeting media JoinUrlLifetimeMinutes must be between 1 and 60.");

    private string CreateToken(string roomName, Guid userId, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || _options.ApiSecret.Length < 32)
        {
            throw new InvalidOperationException("Meeting media credentials are not configured.");
        }

        var header = EncodeJson(new { alg = "HS256", typ = "JWT" });
        var payload = EncodeJson(new
        {
            aud = "jitsi",
            iss = _options.AppId,
            sub = new Uri(GetBaseUrl()).Host,
            room = roomName,
            exp = expiresAtUtc.ToUnixTimeSeconds(),
            context = new { user = new { id = userId.ToString("N") } }
        });
        var signingInput = $"{header}.{payload}";
        var signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.ApiSecret),
            Encoding.UTF8.GetBytes(signingInput));
        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string BuildRoomName(Guid meetingId) => $"us-meeting-{meetingId:N}";

    private static string EncodeJson<T>(T value) => Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(value));

    private static string Base64UrlEncode(byte[] value) => Convert.ToBase64String(value)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private sealed record MediaSessionState(string RoomName, DateTimeOffset ExpiresAtUtc);
}
