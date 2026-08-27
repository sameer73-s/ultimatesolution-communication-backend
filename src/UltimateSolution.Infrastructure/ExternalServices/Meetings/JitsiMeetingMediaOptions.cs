namespace UltimateSolution.Infrastructure.ExternalServices.Meetings;

public sealed class JitsiMeetingMediaOptions
{
    public const string SectionName = "MeetingMedia";
    public string BaseUrl { get; init; } = "https://meet.example.invalid";
    public string AppId { get; init; } = "ultimate-solution-development";
    public string ApiSecret { get; init; } = "development-only-meeting-media-secret-change-before-production";
    public int JoinUrlLifetimeMinutes { get; init; } = 30;
}
