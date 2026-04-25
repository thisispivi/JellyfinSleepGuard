using Jellyfin.Plugin.SleepGuard.Actions;
using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Rules;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

public sealed class SessionMonitorService : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly PlaybackTrackerStore _store;
    private readonly PlaybackEventClassifier _classifier;
    private readonly IReadOnlyList<ISleepRule> _rules;
    private readonly PromptAction _promptAction;
    private readonly PauseAction _pauseAction;
    private readonly StopAction _stopAction;
    private readonly ILogger<SessionMonitorService> _logger;
    private readonly Dictionary<string, Timer> _timers = new(StringComparer.Ordinal);
    private readonly object _timerLock = new();

    public SessionMonitorService(
        ISessionManager sessionManager,
        PlaybackTrackerStore store,
        PlaybackEventClassifier classifier,
        IEnumerable<ISleepRule> rules,
        PromptAction promptAction,
        PauseAction pauseAction,
        StopAction stopAction,
        ILogger<SessionMonitorService> logger)
    {
        _sessionManager = sessionManager;
        _store = store;
        _classifier = classifier;
        _rules = rules.ToArray();
        _promptAction = promptAction;
        _pauseAction = pauseAction;
        _stopAction = stopAction;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _sessionManager.SessionEnded += OnSessionEnded;

        SeedExistingSessions();
        _logger.LogInformation("SleepGuard session monitor started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _sessionManager.SessionEnded -= OnSessionEnded;
        DisposeTimers();
        _store.Dispose();
        _logger.LogInformation("SleepGuard session monitor stopped");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisposeTimers();
    }

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs args)
    {
        RunSafely(() => HandlePlaybackStartAsync(args));
    }

    private void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs args)
    {
        RunSafely(() => HandlePlaybackProgressAsync(args));
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs args)
    {
        RunSafely(() => HandlePlaybackStoppedAsync(args));
    }

    private void OnSessionEnded(object? sender, SessionEventArgs args)
    {
        var sessionId = args.SessionInfo?.Id;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _store.Remove(sessionId);
            CancelTimer(sessionId);
            _logger.LogDebug("Dropped SleepGuard tracker for ended session {SessionId}", sessionId);
        }
    }

    private async Task HandlePlaybackStartAsync(PlaybackProgressEventArgs args)
    {
        var now = DateTimeOffset.UtcNow;
        var playbackEvent = Normalize(args);
        if (playbackEvent is null)
        {
            return;
        }

        var existed = _store.TryGet(playbackEvent.SessionId, out var existing);
        var transition = _classifier.ClassifyStart(existing, playbackEvent, now);
        var tracker = existed && existing is not null ? existing : _store.GetOrAdd(playbackEvent, now);
        tracker.ApplyStart(playbackEvent, transition, now);
        CancelTimer(playbackEvent.SessionId);
        _logger.LogDebug("SleepGuard transition {Transition} for session {SessionId}", transition, playbackEvent.SessionId);
        await EvaluateAsync(tracker, now, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task HandlePlaybackProgressAsync(PlaybackProgressEventArgs args)
    {
        var now = DateTimeOffset.UtcNow;
        var playbackEvent = Normalize(args);
        if (playbackEvent is null)
        {
            return;
        }

        var tracker = _store.GetOrAdd(playbackEvent, now);
        var transition = _classifier.ClassifyProgress(tracker, playbackEvent, now);
        tracker.ApplyProgress(playbackEvent, transition, now);
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        if (configuration.LogProgressEvents)
        {
            _logger.LogInformation(
                "SleepGuard progress session {SessionId}: transition={Transition}, paused={IsPaused}, elapsed={ElapsedSeconds}s, episodes={Episodes}, item={ItemKind}, positionTicks={PositionTicks}",
                playbackEvent.SessionId,
                transition,
                playbackEvent.IsPaused,
                Math.Round(tracker.ContinuousElapsed.TotalSeconds, 1),
                tracker.EpisodesInChain,
                tracker.ItemKind,
                playbackEvent.PositionTicks);
        }
        else
        {
            _logger.LogDebug("SleepGuard transition {Transition} for session {SessionId}", transition, playbackEvent.SessionId);
        }

        await EvaluateAsync(tracker, now, CancellationToken.None).ConfigureAwait(false);
    }

    private Task HandlePlaybackStoppedAsync(PlaybackStopEventArgs args)
    {
        var playbackEvent = Normalize(args);
        if (playbackEvent is null || !_store.TryGet(playbackEvent.SessionId, out var tracker) || tracker is null)
        {
            return Task.CompletedTask;
        }

        tracker.ApplyStopped(DateTimeOffset.UtcNow);
        _logger.LogDebug("SleepGuard transition {Transition} for session {SessionId}", PlaybackTransition.End, playbackEvent.SessionId);
        return Task.CompletedTask;
    }

    private async Task EvaluateAsync(PlaybackTracker tracker, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        if (!configuration.Enabled || tracker.HasPendingOrCompletedAction)
        {
            return;
        }

        foreach (var result in _rules.Select(rule => rule.Evaluate(tracker, configuration, now)))
        {
            if (configuration.LogRuleChecks)
            {
                _logger.LogInformation(
                    "SleepGuard rule check {RuleName} for session {SessionId}: outcome={Outcome}, elapsed={ElapsedSeconds}s, episodes={Episodes}, pendingAction={PendingAction}",
                    result.RuleName,
                    tracker.SessionId,
                    result.Outcome,
                    Math.Round(tracker.ContinuousElapsed.TotalSeconds, 1),
                    tracker.EpisodesInChain,
                    tracker.HasPendingOrCompletedAction);
            }

            if (result.Outcome == SleepRuleOutcome.Blocked)
            {
                return;
            }

            if (result.Outcome == SleepRuleOutcome.Fired)
            {
                _logger.LogInformation("Rule {RuleName} fired for session {SessionId} on device {DeviceId}", result.RuleName, tracker.SessionId, tracker.DeviceId);
                await ExecuteActionsAsync(tracker, configuration, now, cancellationToken).ConfigureAwait(false);
                return;
            }
        }
    }

    private async Task ExecuteActionsAsync(PlaybackTracker tracker, PluginConfiguration configuration, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (configuration.SendPrompt && configuration.PromptGraceSeconds > 0)
        {
            var grace = TimeSpan.FromSeconds(configuration.PromptGraceSeconds);
            try
            {
                await _promptAction.ExecuteAsync(tracker, configuration, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "SleepGuard sent prompt to session {SessionId}; final {Action} scheduled in {GraceSeconds}s",
                    tracker.SessionId,
                    configuration.Action,
                    configuration.PromptGraceSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SleepGuard failed to send prompt to session {SessionId}", tracker.SessionId);
            }

            tracker.MarkPromptPending(now.Add(grace));
            ScheduleFinalAction(tracker.SessionId, grace);
            return;
        }

        await ExecuteFinalActionAsync(tracker.SessionId, cancellationToken).ConfigureAwait(false);
    }

    private void ScheduleFinalAction(string sessionId, TimeSpan dueTime)
    {
        CancelTimer(sessionId);
        lock (_timerLock)
        {
            _timers[sessionId] = new Timer(
                _ => RunSafely(() => ExecuteFinalActionAsync(sessionId, CancellationToken.None)),
                null,
                dueTime,
                Timeout.InfiniteTimeSpan);
        }
    }

    private async Task ExecuteFinalActionAsync(string sessionId, CancellationToken cancellationToken)
    {
        CancelTimer(sessionId);
        if (!_store.TryGet(sessionId, out var tracker) || tracker is null)
        {
            return;
        }

        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var action = configuration.Action == SleepGuardAction.Stop ? (ISleepAction)_stopAction : _pauseAction;
        try
        {
            if (configuration.DryRun)
            {
                tracker.MarkActionIssued(DateTimeOffset.UtcNow, configuration.Action == SleepGuardAction.Pause);
                _logger.LogInformation(
                    "SleepGuard dry run: would send {Action} command to session {SessionId}",
                    configuration.Action,
                    sessionId);
                return;
            }

            var repeatCount = Math.Clamp(configuration.ActionRepeatCount, 1, 5);
            var repeatDelay = TimeSpan.FromSeconds(Math.Clamp(configuration.ActionRepeatIntervalSeconds, 0, 30));
            for (var attempt = 1; attempt <= repeatCount; attempt++)
            {
                await action.ExecuteAsync(tracker, configuration, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "SleepGuard sent {Action} command attempt {Attempt}/{AttemptCount} to session {SessionId}",
                    configuration.Action,
                    attempt,
                    repeatCount,
                    sessionId);

                if (attempt < repeatCount && repeatDelay > TimeSpan.Zero)
                {
                    await Task.Delay(repeatDelay, cancellationToken).ConfigureAwait(false);
                }
            }

            tracker.MarkActionIssued(DateTimeOffset.UtcNow, configuration.Action == SleepGuardAction.Pause);
            _logger.LogInformation("SleepGuard sent {Action} command to session {SessionId}", configuration.Action, sessionId);
        }
        catch (Exception ex)
        {
            tracker.ClearPromptPending();
            _logger.LogWarning(ex, "SleepGuard failed to send {Action} command to session {SessionId}", configuration.Action, sessionId);
        }
    }

    private void SeedExistingSessions()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var session in _sessionManager.GetSessions(Guid.Empty, null!, null, null, false))
        {
            if (session.NowPlayingItem is null)
            {
                continue;
            }

            var playbackEvent = Normalize(session);
            if (playbackEvent is not null)
            {
                _store.GetOrAdd(playbackEvent, now);
            }
        }
    }

    private static PlaybackEvent? Normalize(PlaybackProgressEventArgs args)
    {
        var sessionId = args.Session?.Id;
        var userId = args.Session?.UserId ?? args.Users?.FirstOrDefault()?.Id ?? Guid.Empty;
        var item = args.MediaInfo ?? args.Session?.NowPlayingItem;

        if (string.IsNullOrWhiteSpace(sessionId) || item is null || item.Id == Guid.Empty)
        {
            return null;
        }

        return new PlaybackEvent(
            sessionId,
            userId,
            args.DeviceId ?? args.Session?.DeviceId,
            item.Id,
            item.Type,
            item.SeriesId,
            args.PlaybackPositionTicks,
            args.IsPaused);
    }

    private static PlaybackEvent? Normalize(SessionInfoDto session)
    {
        var item = session.NowPlayingItem;
        if (string.IsNullOrWhiteSpace(session.Id) || item is null || item.Id == Guid.Empty)
        {
            return null;
        }

        return new PlaybackEvent(
            session.Id,
            session.UserId,
            session.DeviceId,
            item.Id,
            item.Type,
            item.SeriesId,
            session.PlayState?.PositionTicks,
            session.PlayState?.IsPaused ?? false);
    }

    private void CancelTimer(string sessionId)
    {
        lock (_timerLock)
        {
            if (_timers.Remove(sessionId, out var timer))
            {
                timer.Dispose();
            }
        }
    }

    private void DisposeTimers()
    {
        lock (_timerLock)
        {
            foreach (var timer in _timers.Values)
            {
                timer.Dispose();
            }

            _timers.Clear();
        }
    }

    private void RunSafely(Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled SleepGuard session monitor error");
            }
        });
    }
}
