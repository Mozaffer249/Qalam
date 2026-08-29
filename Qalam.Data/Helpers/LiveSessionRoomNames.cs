namespace Qalam.Data.Helpers;

public static class LiveSessionRoomNames
{
    public const string Prefix = "qalam-session-";

    public static string ForSchedule(int courseScheduleId) => $"{Prefix}{courseScheduleId}";

    public static bool TryParseScheduleId(string? roomName, out int courseScheduleId)
    {
        courseScheduleId = 0;
        if (string.IsNullOrWhiteSpace(roomName))
            return false;

        if (!roomName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(roomName.AsSpan(Prefix.Length), out courseScheduleId) && courseScheduleId > 0;
    }
}
