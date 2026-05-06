# Contributing to SleepGuard

Thank you for taking the time to contribute. This document covers everything you need to build, test, and submit changes.

## Pre-requisites

| Tool | Minimum version | Notes |
|------|----------------|-------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0 | `dotnet --version` |
| [Node.js](https://nodejs.org/) | 20 LTS | Required to compile the TypeScript overlay |
| [npm](https://www.npmjs.com/) | 10 | Bundled with Node 20 |
| A running Jellyfin instance | 10.11.x | For manual integration testing |

## Cloning and building

```bash
git clone https://github.com/thisispivi/JellyfinSleepGuard
cd JellyfinSleepGuard

# Build everything (client TypeScript is compiled as part of dotnet build)
dotnet build Jellyfin.Plugin.SleepGuard.sln
```

If you prefer make:

```bash
make build
```

## Running tests

```bash
dotnet test Jellyfin.Plugin.SleepGuard.sln
```

Or:

```bash
make test
```

## TypeScript overlay

The overlay lives in `client/sleepguard-overlay.ts`. It is compiled to `client/dist/sleepguard-overlay.js` and then embedded into the DLL by the `.csproj` build.

```bash
cd client
npm ci
npm run build      # compile once
npm run check      # type-check only, no output
```

The `.csproj` `BeforeBuild` target runs `npm ci && npm run build` automatically, so `dotnet build` is enough.

## Deploying to a local Jellyfin instance

### Linux (systemd)

```bash
# Build and publish
dotnet publish src/Jellyfin.Plugin.SleepGuard/Jellyfin.Plugin.SleepGuard.csproj -c Release

# Copy to plugin directory (adjust version as needed)
sudo mkdir -p /var/lib/jellyfin/plugins/SleepGuard_0.1.0.0
sudo cp src/Jellyfin.Plugin.SleepGuard/bin/Release/net9.0/publish/Jellyfin.Plugin.SleepGuard.dll \
       /var/lib/jellyfin/plugins/SleepGuard_0.1.0.0/

# Restart
sudo systemctl restart jellyfin

# Tail logs
journalctl -u jellyfin -f
```

Or use the Makefile shortcut:

```bash
make deploy-local JELLYFIN_VERSION=0.1.0.0
```

### Windows

```powershell
dotnet publish src/Jellyfin.Plugin.SleepGuard/Jellyfin.Plugin.SleepGuard.csproj -c Release
New-Item -ItemType Directory -Force "<jellyfin-data>\plugins\SleepGuard_0.1.0.0"
Copy-Item "src\Jellyfin.Plugin.SleepGuard\bin\Release\net9.0\publish\Jellyfin.Plugin.SleepGuard.dll" `
          "<jellyfin-data>\plugins\SleepGuard_0.1.0.0\"
```

## Quick test cycle

After deploying, configure these values for a fast feedback loop:

```
MaxContinuousMinutes  = 0
MaxContinuousSeconds  = 15   (fires after 15 seconds of playing)
PromptGraceSeconds    = 0    (pause immediately without waiting)
DryRun                = true (log only — no real pause command sent)
LogRuleChecks         = true (see rule evaluation in Jellyfin logs)
```

With `DeveloperMode` enabled, the keyboard shortcut `Ctrl+Shift+Alt+S` force-shows the overlay in the browser without waiting for a rule to fire.

## Branch and commit conventions

| Branch prefix | When to use |
|---------------|-------------|
| `feat/` | New feature or behaviour change |
| `fix/` | Bug fix |
| `chore/` | Build, CI, dependency, or repo maintenance |
| `docs/` | Documentation only |
| `test/` | New or updated tests only |

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(overlay): add keyboard shortcut to force-show in developer mode
fix(tracker): prevent negative ContinuousElapsed on rapid events
chore(ci): add Node 20 setup before dotnet restore
```

## Pull request checklist

- [ ] `dotnet test` passes locally
- [ ] `npm run check` passes in `client/` (TypeScript type-check)
- [ ] `dotnet format --verify-no-changes` passes (CI enforces this)
- [ ] New behaviour is covered by a unit test
- [ ] UI changes include a screenshot in the PR description
- [ ] PR description links a GitHub issue

## Code style

The repository uses `.editorconfig` and Roslyn analyzers. Run `dotnet format` before pushing:

```bash
dotnet format Jellyfin.Plugin.SleepGuard.sln
```

CI will reject PRs where `dotnet format --verify-no-changes` fails.

## Release process

Releases are fully automated. Maintainers push a version tag:

```bash
git tag v0.1.1.0
git push origin v0.1.1.0
```

The `release.yml` workflow builds, tests, packages, updates `manifest.json`, and creates a GitHub release. No manual steps required.
