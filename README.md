<div align="center">
   <div style="display: flex;padding-block:40px;margin-bottom:20px;justify-content:center;background-color:#1f1f1f">
      <picture>
         <img alt="logo" src="./images/logo.png" height="275">
      </picture>
   </div>

# SleepGuard for Jellyfin

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![TypeScript](https://img.shields.io/badge/typescript-%23007ACC.svg?style=for-the-badge&logo=typescript&logoColor=white)
![JavaScript](https://img.shields.io/badge/javascript-%23323330.svg?style=for-the-badge&logo=javascript&logoColor=%23F7DF1E)
![NodeJS](https://img.shields.io/badge/node.js-6DA55F?style=for-the-badge&logo=node.js&logoColor=white)
![NPM](https://img.shields.io/badge/NPM-%23CB3837.svg?style=for-the-badge&logo=npm&logoColor=white)
![Jellyfin](https://img.shields.io/badge/Jellyfin-00A4DC?style=for-the-badge&logo=Jellyfin&logoColor=white)
![AI Generated](https://img.shields.io/badge/AI_Generated-FF69B4?style=for-the-badge&logo=probot&logoColor=white)

</div>

SleepGuard pauses or stops Jellyfin playback when a session looks like it has turned into an overnight autoplay run.

It works from the server side, so the pause/stop action can still fire even if the browser overlay is not installed. For the full "Are you still watching?" overlay in Jellyfin Web, load SleepGuard through a JavaScript Injector plugin.

## Install SleepGuard

1. Open Jellyfin: `Dashboard -> Plugins -> Repositories`.
2. Add this repository URL:

   ```text
   https://raw.githubusercontent.com/thisispivi/JellyfinSleepGuard/main/manifest.json
   ```

3. Install SleepGuard from the plugin catalog.
4. Restart Jellyfin.
5. Open `Dashboard -> Plugins -> SleepGuard`.

The default settings are meant to be usable right away: pause playback after 2 hours of continuous play or after 3 autoplay episodes.

## Basic Settings

Open `Dashboard -> Plugins -> SleepGuard`.

- Enable SleepGuard.
- Choose whether SleepGuard should `Pause` or `Stop`.
- Set the continuous playback limit in minutes.
- Set the autoplay episode limit.
- Set the grace period, which is the delay between the prompt and the final pause/stop action.

Use the Advanced section only if you need user filtering, media type filtering, a night-only time window, or custom prompt text.

## Appearance Settings

Open `Dashboard -> Plugins -> SleepGuard -> Appearance` to customize the browser overlay.

- Choose a solid background, a custom image URL, or the default now-playing artwork background.
- When artwork mode is enabled, SleepGuard uses Jellyfin Web metadata best-effort: series backdrop first, then item backdrop, then primary image.
- Adjust the fallback background, overlay text, continue button, dismiss button, artwork dimming, artwork blur, and prompt panel opacity.
- Use the live preview to check the look before saving.

Artwork backgrounds depend on the Jellyfin Web client exposing current playback metadata to the injected script. If metadata is unavailable, SleepGuard quietly falls back to the configured solid background.

## Overlay Setup

SleepGuard serves its browser overlay at this Jellyfin server endpoint:

```text
/SleepGuard/overlay.js
```

If Jellyfin runs under a URL prefix, for example `/jellyfin/jellyfin`, the real URL is under that prefix:

```text
/jellyfin/jellyfin/SleepGuard/overlay.js
```

To load it in Jellyfin Web:

1. Install a JavaScript Injector plugin for Jellyfin.
2. Open `Dashboard -> Plugins -> JavaScript Injector`.
3. Click `Add Script`.
4. Name it `SleepGuard`.
5. Paste this code into the JavaScript code box:

   ```js
   (() => {
     const endpoint = "SleepGuard/overlay.js";
     const getOverlayUrl = () => {
       if (window.ApiClient && typeof window.ApiClient.getUrl === "function") {
         return window.ApiClient.getUrl(endpoint);
       }

       const webIndex = window.location.pathname.toLowerCase().indexOf("/web");
       const basePath =
         webIndex >= 0 ? window.location.pathname.slice(0, webIndex) : "";
       return `${window.location.origin}${basePath}/${endpoint}`;
     };

     if (document.querySelector('script[data-sleepguard-overlay="true"]'))
       return;

     const script = document.createElement("script");
     script.dataset.sleepguardOverlay = "true";
     script.src = getOverlayUrl();
     script.defer = true;
     script.onerror = () =>
       console.warn("[SleepGuard] Failed to load overlay script:", script.src);
     document.head.appendChild(script);
   })();
   ```

6. Enable the script.
7. Save.
8. Refresh Jellyfin Web in your browser.

SleepGuard's browser overlay is installed through the JavaScript Injector script entry only.

## Simple Test Guide

Use this when you want to confirm SleepGuard is really working without waiting hours.

1. Install SleepGuard and restart Jellyfin.
2. Install a JavaScript Injector plugin.
3. Add the SleepGuard loader script from the Overlay Setup section.
4. Refresh Jellyfin Web.
5. Start playing a video in Jellyfin Web.
6. Open browser developer tools and run:

   ```js
   window.SleepGuardOverlay;
   ```

   If the overlay script loaded, this returns an object with `show`, `hide`, and `settings`.

7. In `Dashboard -> Plugins -> SleepGuard -> Test Mode`:
   - Enable test mode.
   - Set `Continuous seconds` to `15`.
   - Enable `Dry run`.
   - Enable `Log rule checks`.
   - Save.

8. Play something for at least 15 seconds.
9. Check the Jellyfin server log. You should see the continuous-time rule fire and a dry-run line saying SleepGuard would send the pause/stop command.

When you are done testing, disable test mode or set `Continuous seconds` back to `0`.

## Troubleshooting

| Problem                                    | What to check                                                                                                                                         |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| Overlay does not appear                    | Confirm the JavaScript Injector script is enabled, then refresh Jellyfin Web.                                                                         |
| `window.SleepGuardOverlay` is undefined    | The loader script did not run or the overlay failed to load. Check the browser console.                                                               |
| `404` on `/SleepGuard/overlay.js`          | If Jellyfin is under a URL prefix, replace the old loader snippet with the current one above. Also rebuild/reinstall SleepGuard and restart Jellyfin. |
| Prompt appears but playback does not pause | Check whether the client accepts Jellyfin remote pause/stop commands.                                                                                 |
| Nothing happens during testing             | Enable dry run and rule-check logging, then watch the Jellyfin server log while a video plays.                                                        |
| Too many logs                              | Turn off `Log rule checks` after testing.                                                                                                             |

Useful log lines:

| Log line                             | Meaning                                                  |
| ------------------------------------ | -------------------------------------------------------- |
| `SleepGuard session monitor started` | SleepGuard loaded.                                       |
| `Rule ContinuousTimeRule fired`      | The continuous-time limit was reached.                   |
| `Rule AutoplayEpisodeRule fired`     | The autoplay episode limit was reached.                  |
| `SleepGuard sent prompt`             | Jellyfin accepted the prompt/toast request.              |
| `SleepGuard dry run`                 | Test mode dry run is on; no pause/stop command was sent. |
| `SleepGuard sent Pause command`      | SleepGuard sent the media-control command.               |
| `failed to send Pause command`       | The client did not accept the remote command.            |

## Development

Prerequisites: .NET 9 SDK and Node 20.

```powershell
dotnet build Jellyfin.Plugin.SleepGuard.sln
dotnet test Jellyfin.Plugin.SleepGuard.sln
```

For the browser overlay:

```powershell
cd client
npm ci
npm run check
npm run build
```

## License

GPL-2.0-only. See [LICENSE](LICENSE).

