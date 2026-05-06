using Jellyfin.Plugin.SleepGuard.Actions;
using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Rules;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

/// <summary>
/// Hosted service that subscribes to Jellyfin playback events and orchestrates the SleepGuard logic.
/// </summary>
/// <remarks>
/// Evaluation pipeline per event:
/// <list type="number">
///   <item>Normalize raw Jellyfin event args into a <see cref="PlaybackEvent"/>.</item>
///   <item>Classify the event into a <see cref="PlaybackTransition"/> via <see cref="PlaybackEventClassifier"/>.</item>
///   <item>Apply the transition to the session's <see cref="PlaybackTracker"/>.</item>
///   <item>Run all <see cref="IGateRule"/> instances — any <c>Blocked</c> result exits early.</item>
///   <item>Run all <see cref="ITriggerRule"/> instances — first <c>Fired</c> result executes actions.</item>
/// </list>
/// Gate rules (user scope, time window) run before trigger rules (continuous time, autoplay episodes).
/// This ordering is explicit through separate DI registrations, not implicit index ordering.
/// </remarks>
public sealed class SessionMonitorService : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly PlaybackTrackerStore _store;
    private readonly PlaybackEventClassifier _classifier;
    private readonly IReadOnlyList<IGateRule> _gateRules;
    private readonly IReadOnlyList<ITriggerRule> _triggerRules;
    private readonly PromptAction _promptAction;
    private readonly PauseAction _pauseAction;
    private readonly StopAction _stopAction;
    private readonly IPluginConfigurationAccessor _configAccessor;
    private readonly ILogger<SessionMonitorService> _logger;
    private readonly Dictionary<string, Timer> _timers = new(StringComparer.Ordinal);
    private readonly object _timerLock = new();
    private CancellationTokenSource? _cts;

    public SessionMonitorService(
        ISessionManager sessionManager,
        PlaybackTrackerStore store,
        PlaybackEventClassifier classifier,
        IEnumerable<IGateRule> gateRules,
        IEnumerable<ITriggerRule> triggerRules,
        PromptAction promptAction,
        PauseAction pauseAction,
        StopAction stopAction,
        IPluginConfigurationAccessor configAccessor,
        ILogger<SessionMonitorService> logger)
    {
        _sessionManager = sessionManager;
        _store = store;
        _classifier = classifier;
        _gateRules = gateRules.ToArray();
        _triggerRules = triggerRules.ToArray();
        _promptAction = promptAction;
        _pauseAction = pauseAction;
        _stopAction = stopAction;
        _configAccessor = configAccessor;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _sessionManager.SessionEnded += OnSessionEnded;

        SeedExistingSessions();
        StartEvictionTimer();

        _logger.LogInformation("SleepGuard session monitor started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();

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
        _cts?.Dispose();
        DisposeTimers();
    }

    // -------------------------------------------------------------------------
    // Event handlers (fire-and-forget; errors are caught by RunSafely)
    // -------------------------------------------------------------------------

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs args)
        => RunSafely(() => HandlePlaybackStartAsync(args));

    private void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs args)
        => RunSafely(() => HandlePlaybackProgressAsync(args));

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs args)
        => RunSafely(() => HandlePlaybackStoppedAsync(args));

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

    // -------------------------------------------------------------------------
    // Core event handling
    // -------------------------------------------------------------------------

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

        var token = _cts?.Token ?? CancellationToken.None;
        await EvaluateAsync(tracker, now, token).ConfigureAwait(false);
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

        if (transition is PlaybackTransition.ManualPause or PlaybackTransition.ManualResume or PlaybackTransition.Seek or PlaybackTransition.ManualSwitch)
        {
            CancelTimer(playbackEvent.SessionId);
        }

        tracker.ApplyProgress(playbackEvent, transition, now);

        // Snapshot config once for this event cycle — all downstream calls share the same view.
        var configuration = _configAccessor.GetConfiguration();

        _logger.LogDebug("SleepGuard transition {Transition} for session {SessionId}", transition, playbackEvent.SessionId);

        var token = _cts?.Token ?? CancellationToken.None;
        await EvaluateAsync(tracker, configuration, now, token).ConfigureAwait(false);
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

    // -------------------------------------------------------------------------
    // Rule evaluation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Overload used from <c>HandlePlaybackStartAsync</c> which has not yet captured config.
    /// </summary>
    private Task EvaluateAsync(PlaybackTracker tracker, DateTimeOffset now, CancellationToken cancellationToken)
        => EvaluateAsync(tracker, _configAccessor.GetConfiguration(), now, cancellationToken);

    private async Task EvaluateAsync(
        PlaybackTracker tracker,
        PluginConfiguration configuration,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!configuration.Enabled || tracker.HasPendingOrCompletedAction)
        {
            return;
        }

        // Phase 1: gate rules — any Blocked result stops all further evaluation.
        foreach (var gate in _gateRules)
        {
            var result = gate.Evaluate(tracker, configuration, now);
            LogRuleCheck(result, tracker, configuration);
            if (result.Outcome == SleepRuleOutcome.Blocked)
            {
                return;
            }
        }

        // Phase 2: trigger rules — first Fired result executes the action pipeline.
        foreach (var trigger in _triggerRules)
        {
            var result = trigger.Evaluate(tracker, configuration, now);
            LogRuleCheck(result, tracker, configuration);
            if (result.Outcome == SleepRuleOutcome.Fired)
            {
                _logger.LogInformation(
                    "Rule {RuleName} fired for session {SessionId} on device {DeviceId}",
                    result.RuleName, tracker.SessionId, tracker.DeviceId);
                await ExecuteActionsAsync(tracker, configuration, now, cancellationToken).ConfigureAwait(false);
                return;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Action execution
    // -------------------------------------------------------------------------

    private async Task ExecuteActionsAsync(
        PlaybackTracker tracker,
        PluginConfiguration configuration,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (configuration.SendPrompt)
        {
            var graceSeconds = Math.Max(0, configuration.PromptGraceSeconds);
            var grace = TimeSpan.FromSeconds(graceSeconds);

            try
            {
                await _promptAction.ExecuteAsync(tracker, configuration, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "SleepGuard sent prompt to session {SessionId}; final {Action} scheduled in {GraceSeconds}s",
                    tracker.SessionId, configuration.Action, graceSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SleepGuard failed to send prompt to session {SessionId}", tracker.SessionId);
            }

            if (grace <= TimeSpan.Zero)
            {
                await ExecuteFinalActionAsync(tracker.SessionId, cancellationToken).ConfigureAwait(false);
                return;
            }

            tracker.MarkPromptPending(now.Add(grace));
            ScheduleFinalAction(tracker.SessionId, grace);
            return;
        }

        await ExecuteFinalActionAsync(tracker.SessionId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteFinalActionAsync(string sessionId, CancellationToken cancellationToken)
    {
        CancelTimer(sessionId);

        if (!_store.TryGet(sessionId, out var tracker) || tracker is null)
        {
            return;
        }

        // Re-read config at execution time so a config change during the grace period takes effect.
        var configuration = _configAccessor.GetConfiguration();
        var action = configuration.Action == SleepGuardAction.Stop ? (ISleepAction)_stopAction : _pauseAction;

        try
        {
            if (configuration.DryRun)
            {
                tracker.MarkActionIssued(DateTimeOffset.UtcNow, configuration.Action == SleepGuardAction.Pause);
                _logger.LogInformation(
                    "SleepGuard dry run: would send {Action} command to session {SessionId}",
                    configuration.Action, sessionId);
                return;
            }

            await action.ExecuteAsync(tracker, configuration, cancellationToken).ConfigureAwait(false);
            tracker.MarkActionIssued(DateTimeOffset.UtcNow, configuration.Action == SleepGuardAction.Pause);
            _logger.LogInformation("SleepGuard sent {Action} command to session {SessionId}", configuration.Action, sessionId);
        }
        catch (Exception ex)
        {
            tracker.ClearPromptPending();
            _logger.LogWarning(ex, "SleepGuard failed to send {Action} command to session {SessionId}", configuration.Action, sessionId);
        }
    }

    // -------------------------------------------------------------------------
    // Timer management — cancel + create in a single lock to prevent race conditions
    // -------------------------------------------------------------------------

    private void ScheduleFinalAction(string sessionId, TimeSpan dueTime)
    {
        var token = _cts?.Token ?? CancellationToken.None;
        lock (_timerLock)
        {
            if (_timers.Remove(sessionId, out var existing))
            {
                existing.Dispose();
            }

            _timers[sessionId] = new Timer(
                _ => RunSafely(() => ExecuteFinalActionAsync(sessionId, token)),
                null,
                dueTime,
                Timeout.InfiniteTimeSpan);
        }
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

    // -------------------------------------------------------------------------
    // Tracker eviction — removes orphaned trackers (sessions that ended without a SessionEnded event)
    // -------------------------------------------------------------------------

    private void StartEvictionTimer()
    {
        var token = _cts?.Token ?? CancellationToken.None;
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(SessionConstants.EvictionInterval);
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                EvictStaleSessions();
            }
        }, token);
    }

    private void EvictStaleSessions()
    {
        var cutoff = DateTimeOffset.UtcNow - SessionConstants.TrackerEvictionAge;
        var evicted = _store.EvictBefore(cutoff);
        if (evicted > 0)
        {
            _logger.LogInformation("SleepGuard evicted {Count} stale session tracker(s) not accessed since {Cutoff}", evicted, cutoff);
        }
    }

    // -------------------------------------------------------------------------
    // Session seeding and normalisation
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void LogRuleCheck(SleepRuleResult result, PlaybackTracker tracker, PluginConfiguration configuration)
    {
        if (!configuration.LogRuleChecks)
        {
            return;
        }

        _logger.LogInformation(
            "SleepGuard rule check {RuleName} for session {SessionId}: outcome={Outcome}, elapsed={ElapsedSeconds}s, episodes={Episodes}, pendingAction={PendingAction}",
            result.RuleName,
            tracker.SessionId,
            result.Outcome,
            Math.Round(tracker.ContinuousElapsed.TotalSeconds, 1),
            tracker.EpisodesInChain,
            tracker.HasPendingOrCompletedAction);
    }

    private void RunSafely(Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown path — not an error.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled SleepGuard session monitor error");
            }
        }, _cts?.Token ?? CancellationToken.None);
    }
}
