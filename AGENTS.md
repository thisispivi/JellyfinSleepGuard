# Agent instructions

Read [CODING_GUIDELINES.md](./CODING_GUIDELINES.md) completely before editing
code. It is the canonical coding standard for this repository and applies to
every file in the change. Existing deviations are not precedent for new code.

This file holds the workflow rules. `CLAUDE.md` and
`.github/copilot-instructions.md` are thin adapters that point here; do not
copy rules into them.

## Repository layout

- `src/Jellyfin.Plugin.SleepGuard/` is the plugin: `Rules/` decides when a
  session crosses a limit, `Sessions/` tracks playback, `Actions/` pauses or
  stops, `Api/` serves the overlay, and `Configuration/` holds settings and
  the dashboard page.
- `tests/Jellyfin.Plugin.SleepGuard.Tests/` holds the xUnit v3 tests.
- `client/` holds the TypeScript overlay injected into Jellyfin Web.
- `build.yaml` describes the plugin for Jellyfin; `manifest.json` only seeds
  the repository manifest that CI publishes on GitHub Pages.

## Required verification

Every change must pass:

```bash
dotnet format Jellyfin.Plugin.SleepGuard.sln --verify-no-changes
dotnet build Jellyfin.Plugin.SleepGuard.sln -c Release
dotnet test Jellyfin.Plugin.SleepGuard.sln -c Release --no-build
```

For overlay changes, also run `npm ci && npm run check` from `client/`.

## Repository rules

- `targetAbi` in `build.yaml` sets the minimum Jellyfin server. Raise it, and
  the Jellyfin package references, only on purpose, as a major change.
- Never edit `manifest.json` by hand; CI rebuilds the published manifest.

## Versioning and releases

`build.yaml` holds the version, and it is what triggers a release. When
CI passes on `main` and that version has no tag yet, the Release workflow tags
it, publishes the GitHub release with generated notes, attaches the plugin zip, and republishes the plugin manifest on GitHub Pages. Nobody
releases by hand, and nothing is committed back to `main`.

Every change that reaches `main` bumps the version in the same pull request,
following [Semantic Versioning](https://semver.org/):

| Bump  | When                                                                           | Commit type                                  |
| ----- | ------------------------------------------------------------------------------ | -------------------------------------------- |
| major | Breaking: removed or renamed behavior, config, API, or data users must migrate | `feat!:` or a `BREAKING CHANGE:` footer      |
| minor | A new user-visible feature                                                     | `feat:`                                      |
| patch | A fix, performance work, refactor, or dependency update                        | `fix:`, `perf:`, `refactor:`, `chore(deps):` |
| none  | Docs, tests, CI, or agent configuration only                                   | `docs:`, `test:`, `ci:`, `chore:`            |

- Compare against the last tag (`git describe --tags --abbrev=0`) and pick
  the highest bump the change needs. Bump once per pull request.
- Versions use the Jellyfin plugin form `MAJOR.MINOR.PATCH.0`. Bump
  `version` in `build.yaml` and `<Version>` in the plugin `.csproj` together.
- Never create tags, GitHub releases, or changelog entries yourself.
- The pull request title is the release note. The "Version bump" check fails
  when code changes without a bump, unless the title type is `docs`, `test`,
  `ci`, `style`, `build`, or `chore` (but not `chore(deps)`).

## Commits and pull requests

- Use [Conventional Commits](https://www.conventionalcommits.org/). Name
  branches `<type>/<short-description>`.
- The repository owner is the only author. Never add `Co-Authored-By`
  trailers, "Generated with" lines, or any other attribution to commits or pull
  requests.
- Do not bypass git hooks or CI checks.

## Security

Dependabot alerts and security updates are on. To triage and fix them, use the
`fix-vulnerabilities` skill in `.claude/skills/fix-vulnerabilities/`.
