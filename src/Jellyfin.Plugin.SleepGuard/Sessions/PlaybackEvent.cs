using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

public sealed record PlaybackEvent(
    string SessionId,
    Guid UserId,
    string? DeviceId,
    Guid ItemId,
    BaseItemKind ItemKind,
    Guid? SeriesId,
    long? PositionTicks,
    bool IsPaused);
