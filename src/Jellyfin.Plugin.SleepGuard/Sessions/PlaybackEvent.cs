using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

/// <summary>
/// Normalized view of a Jellyfin playback event, independent of the raw event-args type it came from.
/// </summary>
/// <param name="SessionId">Jellyfin session ID.</param>
/// <param name="UserId">User that owns the session, or <see cref="Guid.Empty"/> when unknown.</param>
/// <param name="DeviceId">Client device ID, when reported.</param>
/// <param name="ItemId">ID of the item being played.</param>
/// <param name="ItemKind">Kind of the item being played.</param>
/// <param name="SeriesId">Parent series ID when the item is an episode.</param>
/// <param name="PositionTicks">Playback position in ticks, when reported.</param>
/// <param name="IsPaused">Whether the client reports playback as paused.</param>
public sealed record PlaybackEvent(
    string SessionId,
    Guid UserId,
    string? DeviceId,
    Guid ItemId,
    BaseItemKind ItemKind,
    Guid? SeriesId,
    long? PositionTicks,
    bool IsPaused);
