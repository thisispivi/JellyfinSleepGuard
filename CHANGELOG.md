# Changelog

All notable changes to SleepGuard are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning follows Jellyfin plugin conventions: `MAJOR.MINOR.PATCH.BUILD`.

## [Unreleased]

### Added
- Three-section settings page: **Behavior**, **Customization**, and **Developer Tools** tabs.
- `DeveloperMode` configuration property that unlocks the Developer Tools tab.
- Keyboard shortcut `Ctrl+Shift+Alt+S` to force-show the overlay during development (Developer Mode only).
- Inline live-preview panel in the Customization tab for real-time overlay appearance feedback.
- `GET /SleepGuard/config/developer-mode` endpoint for the overlay to read the developer flag.
- `IPluginConfigurationAccessor` interface replacing all `Plugin.Instance` static accesses.
- `PluginConfigurationValidator` that logs warnings and clamps invalid config values instead of silently accepting them.
- `SessionConstants` class centralising all hardcoded session-timing values.
- `IGateRule` and `ITriggerRule` interfaces replacing the shared `ISleepRule` interface.
- TTL-based eviction for orphaned `PlaybackTracker` entries (4-hour idle threshold).
- `[SettingsGroup]` attribute to annotate every config property with its UI section.
- TypeScript source for the overlay script with strict type checking.
- `CONTRIBUTING.md`, `SECURITY.md`, `docs/ARCHITECTURE.md`, GitHub issue templates, and PR template.
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

## [0.1.0.11] — 2025-01-01

### Added
- Initial release with continuous-time rule, autoplay-episode rule, time-window gate, and user-scope gate.
- Optional full-screen Jellyfin Web overlay at `/SleepGuard/overlay.js`.
- English and Italian settings page and overlay translations.
- Embedded SleepGuard logo for the plugin settings page.
