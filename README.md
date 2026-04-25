# SleepGuard for Jellyfin

[![Build](https://github.com/thisispivi/JellyfinSleepGuard/actions/workflows/build.yml/badge.svg)](https://github.com/thisispivi/JellyfinSleepGuard/actions/workflows/build.yml)
[![Latest release](https://img.shields.io/github/v/release/thisispivi/JellyfinSleepGuard)](https://github.com/thisispivi/JellyfinSleepGuard/releases)
![Jellyfin](https://img.shields.io/badge/Jellyfin-10.10%2B-blue)
![License](https://img.shields.io/badge/license-GPL--2.0--only-blue)

SleepGuard is a server-side Jellyfin plugin that pauses or stops playback when a session looks like it has turned into an overnight autoplay run.

## Why

Jellyfin does not currently include a Netflix-style "Are you still watching?" prompt. If someone falls asleep during a binge, playback can continue for hours, wasting bandwidth and leaving the user far from their real resume position. SleepGuard watches active sessions on the server and acts when configurable limits are reached.

## Screenshots

Admin configuration and triggered-pause screenshots will be added before the first release.

## Features

- Continuous playback limit, measured in wallclock playing time.
- Autoplay episode limit for episode chains in the same series.
- Optional active time window such as `22:00` to `07:00`.
- Pause or stop action when a rule fires.
- Best-effort Jellyfin client toast using `SendMessageCommand`.
- Per-user scope with all-users, whitelist, and blacklist modes.
- Configurable opt-outs for movies, music, and Live TV.

## Compatibility

| Component | Support |
|---|---|
| Jellyfin Server | 10.10.x and newer 10.x releases using the same plugin ABI |
| Target framework | .NET 8.0 |
| Web client prompt | Supported |
| iOS prompt | Expected to work where message commands are honored |
| Android TV prompt | Toast may not appear; pause or stop still works |
| Other clients | Pause or stop depends on standard Jellyfin media-control support |

## Installation

Plugin repository installation will be available after the first release. Add the repository URL in Dashboard -> Plugins -> Repositories, then install SleepGuard from the catalog.

Manual installation:

1. Download the release `.zip`.
2. Extract it into `<jellyfin-data>/plugins/SleepGuard_<version>/`.
3. Restart Jellyfin.
4. Open Dashboard -> Plugins -> SleepGuard.

## Configuration

| Setting | Default | Meaning |
|---|---:|---|
| `Enabled` | `true` | Master switch. |
| `Action` | `Pause` | Send pause or stop when a rule fires. |
| `MaxContinuousMinutes` | `120` | Continuous playing minutes before action; `0` disables. |
| `MaxAutoplayEpisodes` | `3` | Episode-chain count before action; `0` disables. |
| `OnlyWithinTimeWindow` | `false` | Only evaluate limits inside the configured clock window. |
| `TimeWindowStart` | `22:00` | Server-local window start. |
| `TimeWindowEnd` | `07:00` | Server-local window end; midnight wrap is supported. |
| `IncludeMovies` | `true` | Apply continuous-time limits to movies. |
| `IncludeMusic` | `false` | Apply continuous-time limits to music. |
| `IncludeLiveTv` | `false` | Apply continuous-time limits to Live TV. |
| `UserMode` | `AllUsers` | All users, whitelist, or blacklist. |
| `UserIds` | empty | User IDs used by whitelist or blacklist mode. |
| `SendPrompt` | `true` | Send a best-effort client message before action. |
| `PromptMessage` | `Are you still watching?` | Text sent to clients that support messages. |
| `PromptGraceSeconds` | `30` | Delay after prompt before pause or stop; `0` acts immediately. |

## How It Works

```mermaid
flowchart LR
  A["Jellyfin session events"] --> B["SessionMonitorService"]
  B --> C["PlaybackEventClassifier"]
  C --> D["PlaybackTracker"]
  D --> E["Sleep rules"]
  E --> F["PromptAction"]
  E --> G["PauseAction or StopAction"]
  F --> H["ISessionManager.SendMessageCommand"]
  G --> I["ISessionManager.SendPlaystateCommand"]
```

The plugin subscribes to playback events in a hosted service, keeps one in-memory tracker per Jellyfin session, evaluates gate and trigger rules after each update, then sends Jellyfin session commands when a limit fires.

## Development

One-command repository build:

```powershell
.\scripts\Build-PluginRepository.ps1 -Version 0.1.0.0
```

Build, test, generate `manifest.json`, commit/push it, and create the GitHub release with the plugin zip:

```powershell
.\scripts\Build-PluginRepository.ps1 -Version 0.1.0.0 -CommitAndPush -CreateRelease
```

For the first release from a fresh repo, include all project files in the commit:

```powershell
.\scripts\Build-PluginRepository.ps1 -Version 0.1.0.0 -CommitAndPush -StageAll -CreateRelease
```

The Jellyfin repository URL is:

```text
https://raw.githubusercontent.com/thisispivi/JellyfinSleepGuard/main/manifest.json
```

Build:

```powershell
dotnet build Jellyfin.Plugin.SleepGuard.sln
```

Run tests:

```powershell
dotnet test Jellyfin.Plugin.SleepGuard.sln
```

Publish for a local Jellyfin server:

```powershell
dotnet publish src/Jellyfin.Plugin.SleepGuard/Jellyfin.Plugin.SleepGuard.csproj -c Release
New-Item -ItemType Directory -Force "<jellyfin-data>\plugins\SleepGuard_0.1.0.0"
Copy-Item "src\Jellyfin.Plugin.SleepGuard\bin\Release\net8.0\publish\Jellyfin.Plugin.SleepGuard.dll" "<jellyfin-data>\plugins\SleepGuard_0.1.0.0\"
```

Restart Jellyfin after copying the DLL. For live testing, set `MaxContinuousMinutes=2`, `MaxAutoplayEpisodes=2`, and `PromptGraceSeconds=10`, then watch the Jellyfin logs while playing from a web client.

## Troubleshooting

| Log line | Meaning |
|---|---|
| `SleepGuard session monitor started` | The hosted service loaded and subscribed to session events. |
| `Rule ContinuousTimeRule fired` | The continuous-time threshold was reached. |
| `Rule AutoplayEpisodeRule fired` | The episode-chain threshold was reached. |
| `failed to send prompt` | The client may not support message commands; pause or stop can still work. |
| `failed to send Pause command` | The client session did not accept remote media control. |

## Contributing

Issues and pull requests are welcome. Please keep changes focused, run `dotnet test`, and follow the analyzer and `.editorconfig` settings in the repository.

## Roadmap

- Persist trackers across server restarts.
- Per-user thresholds.
- Optional companion web UI for a real "Yes, I am here" button.

## License

GPL-2.0-only. See [LICENSE](LICENSE).
