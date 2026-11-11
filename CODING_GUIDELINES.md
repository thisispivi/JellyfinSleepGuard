# Coding Guidelines

The canonical standard for every change, by a person or an agent. `AGENTS.md`
holds the workflow and the quality gate; this file holds how the code is
written. Existing deviations are not precedent.

## Principles

- Prefer the least code that fully solves the task. Use what Jellyfin and the
  .NET base library already provide before adding a package.
- Keep decisions pure: rules take a session snapshot and settings and return a
  decision, without touching Jellyfin services, so they stay easy to test.
- Comments explain why, never what.

## C#

- .NET 10, nullable reference types on, file-scoped namespaces, one public
  type per file, named after the file.
- `.editorconfig` and `dotnet format` define formatting; the build enforces
  code style.
- `PascalCase` for types, methods, and properties; `_camelCase` for private
  fields; `camelCase` for locals and parameters. Async methods end in `Async`
  and take a `CancellationToken` when they do I/O.
- Depend on Jellyfin services through constructor injection, registered in
  `PluginServiceRegistrator`.
- Log through `ILogger<T>` with message templates, never string
  interpolation. Never log user tokens.

## Configuration and API

- Settings live in `Configuration/PluginConfiguration.cs` with safe defaults
  that work without any change. Validate values where they are read.
- API endpoints require Jellyfin authentication and return only what the
  overlay needs.

## Overlay (`client/`)

- TypeScript in strict mode, no runtime dependencies. It runs inside Jellyfin
  Web, so it must fail quietly and never break playback.

## Tests

- Every behavior change and bug fix comes with an xUnit test. Use fakes for
  Jellyfin services and an injected clock; never sleep in a test.
