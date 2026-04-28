using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SleepGuard.Api;

/// <summary>
/// Serves the SleepGuard overlay script with embedded plugin configuration.
/// Jellyfin discovers plugin controllers via <c>AddApplicationPart</c> at startup.
/// </summary>
[ApiController]
[Route("SleepGuard")]
public sealed class SleepGuardController : ControllerBase
{
    /// <summary>
    /// Returns the overlay JavaScript with the current plugin settings
    /// prepended as <c>window.__SLEEPGUARD_CONFIG__</c>.
    /// </summary>
    /// <returns>The combined overlay script.</returns>
    [HttpGet("overlay.js")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetOverlayScript()
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null)
        {
            return NotFound("SleepGuard plugin is not loaded.");
        }

        var script = OverlayScriptBuilder.Build(config);
        if (script is null)
        {
            return NotFound("Embedded overlay resource is missing.");
        }

        Response.Headers.CacheControl = "no-cache, no-store";
        return Content(script, "application/javascript");
    }
}
