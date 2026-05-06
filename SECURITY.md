# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| Latest release | ✅ |
| Older releases | ❌ — please upgrade |

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Use [GitHub's private security advisory feature](https://github.com/thisispivi/JellyfinSleepGuard/security/advisories/new) to report privately. You will receive a response within 7 days.

Include:
- A description of the vulnerability and its potential impact
- Steps to reproduce
- Your Jellyfin version and SleepGuard version
- Any mitigations you are aware of

## Scope

SleepGuard is a server-side Jellyfin plugin. Its security surface is limited:

| Area | Notes |
|------|-------|
| **Session commands** | The plugin sends `Pause` and `Stop` playstate commands via Jellyfin's `ISessionManager`. It cannot read media files, modify library data, or access the filesystem outside of its own config directory. |
| **Overlay endpoint** | `GET /SleepGuard/overlay.js` is `[AllowAnonymous]` by design — the Jellyfin JavaScript Injector fetches scripts before an auth token is established. The response contains only appearance configuration; no user PII is included. |
| **Configuration** | Plugin configuration is stored as XML in Jellyfin's plugin data directory. SleepGuard reads it at startup and on every request. Tampering with this file requires filesystem access to the Jellyfin data directory, which implies a higher level of compromise. |
| **Developer Mode** | When enabled, a keyboard shortcut becomes active in the browser and additional diagnostic settings are visible. Developer Mode should be disabled on production servers. |

## Out of scope

- Vulnerabilities in Jellyfin itself — report those to the [Jellyfin project](https://jellyfin.org/docs/general/contributing/issues.html).
- Vulnerabilities in the Jellyfin JavaScript Injector plugin — report those to its maintainer.
- Issues that require an existing Jellyfin admin account to exploit (admin accounts are already trusted).
