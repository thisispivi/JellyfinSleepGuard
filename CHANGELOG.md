# Changelog

All notable changes to SleepGuard are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning follows Jellyfin plugin conventions: `MAJOR.MINOR.PATCH.BUILD`.

## [Unreleased]

### Removed
- `scripts/Build-PluginRepository.ps1`: releases are cut by pushing a `vX.Y.Z.B` tag. The script rebuilt `manifest.json` with a single version, which dropped older entries and duplicated the one written by the release workflow.
- Write-only `PlaybackTracker.StartedAtUtc` and `PlaybackTracker.LastUserActionUtc`.
- Overlay `pauseWhenShown` setting, which was always `true`.
- Settings page fallback to a plugin ID that SleepGuard no longer uses.

## [0.2.0.0] — 2026-09-30

### Changed
- **Breaking:** targets Jellyfin 12 (`targetAbi` 12.0.0.0, .NET 10). Jellyfin 10.11 servers stay on SleepGuard 0.1.0.22.
- Logo image reduced from 1672×941 (1.3 MB) to 800×450, and shown smaller on the settings page.
- Public API is fully XML-documented; the `CS1591` suppression is gone.

### Fixed
- Plugin image missing in the Jellyfin plugin list: the assembly version was pinned to `0.1.0.0`, so Jellyfin requested the image for a version that was not installed.
- Settings page rendered without styles: the stylesheet lived in `<head>`, which Jellyfin Web discards for plugin pages.
- Settings page did not load or save on Jellyfin 12: Jellyfin Web rewrites `${...}` placeholders in plugin pages, which corrupted the script's template literals and the copyable loader snippet.
- `PluginConfigurationValidator` was registered but never invoked; it now runs at startup.

## [0.1.0.12 – 0.1.0.22]

### Added
- Simplified settings page with **Main**, **Advanced**, and **Test Mode** sections.
- Manual JavaScript Injector setup snippet for loading `/SleepGuard/overlay.js`.
- `IPluginConfigurationAccessor` interface replacing all `Plugin.Instance` static accesses.
- `PluginConfigurationValidator` that logs warnings and clamps invalid config values instead of silently accepting them.
- `SessionConstants` class centralising all hardcoded session-timing values.
- `IGateRule` and `ITriggerRule` interfaces replacing the shared `ISleepRule` interface.
- TTL-based eviction for orphaned `PlaybackTracker` entries (4-hour idle threshold).
- TypeScript source for the overlay script with strict type checking.
- `Makefile` with Unix-friendly build targets including `deploy-local` for systemd-hosted Jellyfin.

### Changed
- Overlay script is now compiled from TypeScript (`client/sleepguard-overlay.ts`).
- `.csproj` runs `npm ci && npm run build` before embedding the overlay resource.
- CI (`build.yml`, `release.yml`) sets up Node 20 before `dotnet restore`.
- `ScheduleFinalAction` collapses cancel + create into a single lock acquisition, closing a timer race condition.
- `CancellationToken` is now propagated from event handlers through the full evaluation pipeline.
- Config is snapshotted once per event cycle instead of being re-read three times.

### Fixed
- XSS: overlay no longer uses `innerHTML` for user-controlled prompt text.
- `MutationObserver` is now disconnected before showing the overlay and reconnected after dismissal, preventing a memory leak on long sessions.
- `video.play()` errors are now logged via `console.warn` instead of being silently swallowed.
- Polymer-specific play-button selector removed from the overlay's resume-playback fallback chain.

### Removed
- `Diagnostics/LogScopes.cs` — dead code, never called.
- `ISleepRule` — replaced by `IGateRule` and `ITriggerRule`.
- Browser overlay test shortcut and settings-page overlay test button.
- Unused diagnostics endpoint, config grouping attribute, repeat-action controls, progress-event logging, prompt-timeout setting, and overlay appearance config.

## [0.1.0.11] — 2025-01-01

### Added
- Initial release with continuous-time rule, autoplay-episode rule, time-window gate, and user-scope gate.
- Optional full-screen Jellyfin Web overlay at `/SleepGuard/overlay.js`.
- English and Italian settings page and overlay translations.
- Embedded SleepGuard logo for the plugin settings page.
