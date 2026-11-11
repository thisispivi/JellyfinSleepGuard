# SleepGuard architecture

The internal structure of the SleepGuard plugin, for contributors and maintainers. For end-user setup, see [README.md](../README.md).

## Layer overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Jellyfin Server                                                │
│  ┌──────────────┐   playback events   ┌──────────────────────┐ │
│  │ ISessionMgr  │ ──────────────────▶ │ SessionMonitorService│ │
│  │ IUserMgr     │                     │  (IHostedService)    │ │
│  └──────────────┘                     └──────────┬───────────┘ │
│                                                  │             │
│       ┌──────────────────────────────────────────┤             │
│       ▼                                          ▼             │
│  ┌──────────────┐   ┌──────────────┐   ┌────────────────────┐ │
│  │ PlaybackEvent│   │ PlaybackTracker   │   Rule pipeline    │ │
│  │ Classifier   │──▶│ Store        │──▶│  IGateRule[]       │ │
│  └──────────────┘   └──────────────┘   │  ITriggerRule[]    │ │
│                                         └────────┬───────────┘ │
│                                                  │  rule fired │
│                                 ┌────────────────┤             │
│                                 ▼                ▼             │
│                          ┌─────────────┐  ┌──────────────┐    │
│                          │PromptAction │  │PauseAction / │    │
│                          │             │  │StopAction    │    │
│                          └──────┬──────┘  └──────┬───────┘    │
│                                 │                │             │
│                    SendMessage  │  grace period  │ SendPlaystate
│                    Command      ▼                ▼             │
│                          ┌─────────────────────────────────┐  │
│                          │   ISessionManager (Jellyfin)    │  │
│                          └─────────────────────────────────┘  │
│                                                                 │
│  HTTP layer:                                                    │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ SleepGuardController                                      │ │
│  │   GET /SleepGuard/overlay.js  (AllowAnonymous)            │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Key types

| Type                         | Role                                                                                                   |
| ---------------------------- | ------------------------------------------------------------------------------------------------------ |
| `SessionMonitorService`      | `IHostedService`; subscribes to Jellyfin events, drives the full evaluation pipeline per progress tick |
| `PlaybackEventClassifier`    | Classifies each raw event as Start / Progress / Stopped / Seek / Pause / Resume                        |
| `PlaybackTrackerStore`       | Thread-safe dictionary of `PlaybackTracker` keyed by Jellyfin session ID; TTL eviction every 30 min    |
| `PlaybackTracker`            | Per-session state: accumulated playing time, episode count, last-action timestamp                      |
| `IGateRule`                  | Short-circuits evaluation when returning `Blocked` (UserScopeRule, TimeWindowRule)                     |
| `ITriggerRule`               | Fires actions when returning `Fired` (ContinuousTimeRule, AutoplayEpisodeRule)                         |
| `PromptAction`               | Sends `SendMessageCommand` to the client; schedules the grace-period timer                             |
| `PauseAction` / `StopAction` | Sends `SendPlaystateCommand` to the client                                                             |
| `SleepGuardController`       | ASP.NET Core controller; serves the overlay script                                                     |
| `OverlayScriptBuilder`       | Reads the embedded `overlay.js` template, prepends `window.__SLEEPGUARD_CONFIG__`                      |

## Rule evaluation pipeline

Every playback progress event triggers the following sequence inside `SessionMonitorService.EvaluateAsync`:

```
1. Snapshot config once  (IPluginConfigurationAccessor.GetConfiguration())
   └─ Config is read exactly once; the same snapshot flows through the whole evaluation.

2. Gate rules  (IEnumerable<IGateRule>)
   ├─ UserScopeRule   — blocks if the session user is outside the configured scope
   └─ TimeWindowRule  — blocks if the current server time is outside the configured window
   If any gate returns Blocked → return immediately; no trigger rules run.

3. Trigger rules  (IEnumerable<ITriggerRule>)
   ├─ ContinuousTimeRule   — fires when accumulated playing time exceeds the threshold
   └─ AutoplayEpisodeRule  — fires when consecutive same-series episodes exceed the limit
   First rule to return Fired → ExecuteActionsAsync → return.
   If no rule fires → return (no action).

4. ExecuteActionsAsync
   ├─ SendPrompt=true  → PromptAction.Execute()
   │    └─ SendMessageCommand (best-effort; failure is logged, not thrown)
   │    └─ ScheduleFinalAction(graceSeconds)
   └─ SendPrompt=false → ExecuteFinalActionAsync() immediately
```

Gate rules always run before trigger rules. The two-phase loop guarantees this, whatever the registration order. A new gate rule only needs to implement `IGateRule` and be registered; it then blocks trigger evaluation when it returns `Blocked`.

Config is captured once and never re-read during an evaluation. If the admin saves new settings during an evaluation cycle, the change is picked up on the next progress event.

## Configuration lifecycle

```
Plugin XML file  ─────▶  Jellyfin deserialization  ─────▶  PluginConfiguration
(on disk)                                                    (in-memory object)
                                                                    │
                                                                    ▼
                                                     PluginConfigurationValidator
                                                     .Validate(config)
                                                     (called once at startup)
                                                                    │
                                                                    ▼
                                                     IPluginConfigurationAccessor
                                                     .GetConfiguration()
                                                     (called once per evaluation tick)
```

`PluginConfiguration` is a flat class that inherits `BasePluginConfiguration`. It stays flat on purpose: nesting into sub-classes would change the XML element hierarchy and silently wipe existing user configs on upgrade.

`IPluginConfigurationAccessor` is the only DI-registered interface that accesses `Plugin.Instance`, and all other services receive it through constructor injection. `Plugin.Instance` is therefore touched in exactly one place: `PluginServiceRegistrator`.

`PluginConfigurationValidator` runs once when `SessionMonitorService` starts. It clamps out-of-range values to safe defaults and logs warnings. It never throws; the plugin must not fail to start because of a bad config value.

## Overlay injection model

```
Browser
  │
  │ 1. A JavaScript Injector script entry appends SleepGuard/overlay.js
  │    to the Jellyfin Web page, preserving any Jellyfin URL prefix
  │
  ▼
GET <jellyfin-base>/SleepGuard/overlay.js  [AllowAnonymous]
  │
  ▼
SleepGuardController.GetOverlayScript()
  ├─ IPluginConfigurationAccessor.GetConfiguration()
  └─ OverlayScriptBuilder.Build(config)
       ├─ Reads embedded resource: Jellyfin.Plugin.SleepGuard.Api.overlay.js
       │    (compiled from client/sleepguard-overlay.ts by dotnet build)
       └─ Prepends:  window.__SLEEPGUARD_CONFIG__ = { ... };
            (prompt text, language, appearance settings, ...)
  Response headers: Cache-Control: no-cache, no-store
  │
  ▼
Browser executes the IIFE
  ├─ Reads window.__SLEEPGUARD_CONFIG__
  ├─ Starts MutationObserver watching document.body
  │    (disconnects while overlay is displayed; reconnects on dismiss)
  ├─ Applies appearance settings as CSS variables
  ├─ Optionally resolves current Jellyfin artwork through window.ApiClient
  │    (best-effort; falls back to the configured solid background)
  └─ Exposes window.SleepGuardOverlay = { show, hide, settings }
```

The endpoint is `AllowAnonymous` because the JavaScript Injector loader requests the script from Jellyfin Web without adding a SleepGuard-specific token. The response contains only overlay display settings.

The response is `no-cache, no-store` so prompt text and language changes show on the next page load without the admin clearing the browser cache. The settings are baked in at serve time, and a cached response would show stale config.

The config travels in `window.__SLEEPGUARD_CONFIG__`, in the same request as the script. A separate fetch would let the overlay initialise with defaults first and then load its config, which causes a FOUC/race condition.

The overlay has no server endpoint for artwork. In Jellyfin Web it uses the existing `window.ApiClient` session and image helpers to find the current item or series backdrop. This is best-effort on purpose, because Jellyfin Web internals can change across versions and an injected script has to tolerate that; missing metadata leaves the solid fallback background in place.

The overlay source lives in `client/sleepguard-overlay.ts`. The `dotnet build` MSBuild target `BuildClientAssets` runs `npm ci && npm run build` (TypeScript → `client/dist/sleepguard-overlay.js`) before the C# compilation step. The compiled file is embedded into the DLL with logical name `Jellyfin.Plugin.SleepGuard.Api.overlay.js`.

## Settings page

`Configuration/configPage.html` is a legacy Jellyfin plugin page. Jellyfin Web fetches it, keeps only the `div[data-role="page"]` element, and discards everything else, so the `<style>` and `<script>` blocks must live inside that element. Anything placed in `<head>` never reaches the browser.

Jellyfin Web also runs its translation pass over the page and replaces every `${key}` placeholder with a localized string. The page script therefore must not use JavaScript template literals with `${...}`; use string concatenation instead.

The logo shown on the page and in the plugin catalog is `images/logo.png`. Jellyfin serves the catalog image at `/Plugins/{id}/{version}/Image` using the assembly version, so the DLL must be built with the same version as the manifest entry (`/p:Version=...`); the project file deliberately does not pin `AssemblyVersion`.

## Test mode contract

Test Mode uses only server-side settings: `MaxContinuousSeconds`, `DryRun`, and `LogRuleChecks`. The settings page treats Test Mode as enabled when any of those values are active, and saving with Test Mode disabled clears all three.

The overlay does not register a browser shortcut or call a diagnostics endpoint.

## Adding a trigger rule

1. Create the rule class in `src/.../Rules/`:

   ```csharp
   public sealed class MyNewRule : ITriggerRule
   {
       public string Name => nameof(MyNewRule);

       public SleepRuleResult Evaluate(
           PlaybackTracker tracker,
           PluginConfiguration configuration,
           DateTimeOffset nowUtc)
       {
           return /* condition */
               ? SleepRuleResult.Fired(Name)
               : SleepRuleResult.None(Name);
       }
   }
   ```

2. Register it in `PluginServiceRegistrator.cs`:

   ```csharp
   serviceCollection.AddSingleton<ITriggerRule, MyNewRule>();
   ```

3. Add configuration if needed: new property in `PluginConfiguration` and a corresponding field in `configPage.html`.

4. Write a test in `tests/...` that verifies the rule returns `Fired` and `None` at the expected boundary values.

To add a gate rule instead, implement `IGateRule` and register with `AddSingleton<IGateRule, MyGateRule>()`. Gate rules are evaluated before all trigger rules.

## Adding a language

1. `PluginConfiguration.cs`: the `Language` property already accepts any string, so it needs no code change unless the value needs validation.

2. `client/sleepguard-overlay.ts`: add a new entry to the `translations` object:

   ```typescript
   const translations: Record<SupportedLanguage, TranslationSet> = {
       en: { ... },
       it: { ... },
       fr: {                               // ← add
           promptText:         "Regardez-vous toujours ?",
           headerText:         "SleepGuard",
           continueButtonText: "Continuer",
           dismissButtonText:  "Rester en pause",
       },
   };
   ```

   Also extend `SupportedLanguage = "en" | "it" | "fr"`.

3. `configPage.html`: add the new language key to:
   - The `<select id="SleepGuardLanguage">` options.
   - The `translations.fr` object in the page's i18n section (tab labels, section headers, etc.).

4. Run `npm run check` in `client/` to confirm the TypeScript is still valid, then `dotnet build` to verify the overlay embeds correctly.
