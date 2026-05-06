using Jellyfin.Plugin.SleepGuard.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SleepGuard.Api;

/// <summary>
/// Serves SleepGuard assets and configuration endpoints.
/// Jellyfin discovers plugin controllers via <c>AddApplicationPart</c> at startup.
/// </summary>
[ApiController]
[Route("SleepGuard")]
public sealed class SleepGuardController : ControllerBase
{
    private readonly IPluginConfigurationAccessor _configAccessor;

    public SleepGuardController(IPluginConfigurationAccessor configAccessor)
    {
        _configAccessor = configAccessor;
    }

    /// <summary>
    /// Returns the overlay JavaScript with the current plugin settings prepended as
    /// <c>window.__SLEEPGUARD_CONFIG__</c>.
    /// </summary>
    /// <remarks>
    /// <c>[AllowAnonymous]</c> is required because the Jellyfin JavaScript Injector
    /// fetches this script before an authenticated session is established in the browser.
    /// The response contains only appearance / UX settings; no user PII is included.
    /// </remarks>
    [HttpGet("overlay.js")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetOverlayScript()
    {
        var config = _configAccessor.GetConfiguration();
        var script = OverlayScriptBuilder.Build(config);
        if (script is null)
        {
            return NotFound("Embedded overlay resource is missing.");
        }

        Response.Headers.CacheControl = "no-cache, no-store";
        return Content(script, "application/javascript");
    }

    /// <summary>
    /// Returns whether Developer Mode is currently enabled.
    /// The overlay reads this at load time to decide whether to register the keyboard shortcut.
    /// </summary>
    [HttpGet("config/developer-mode")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDeveloperMode()
    {
        var config = _configAccessor.GetConfiguration();
        return Ok(new { developerMode = config.DeveloperMode });
    }
}
