namespace UltimateSolution.Infrastructure.SignalR;

public static class HubGroupNames
{
    public static string Channel(Guid channelId) => $"channel:{channelId:N}";

    public static string User(Guid userId) => $"user:{userId:N}";
}
