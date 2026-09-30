using Jellyfin.Plugin.SleepGuard.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SleepGuard.Api;

/// <summary>
/// Serves the SleepGuard browser overlay script.
/// Jellyfin discovers plugin controllers via <c>AddApplicationPart</c> at startup.
/// </summary>
[ApiController]
[Route("SleepGuard")]
public sealed class SleepGuardController : ControllerBase
{
    private readonly IPluginConfigurationAccessor _configAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SleepGuardController"/> class.
    /// </summary>
    /// <param name="configAccessor">Accessor for the current plugin configuration.</param>
    public SleepGuardController(IPluginConfigurationAccessor configAccessor)
    {
        _configAccessor = configAccessor;
    }

    /// <summary>
    /// Returns the overlay JavaScript with the current plugin settings prepended as
    /// <c>window.__SLEEPGUARD_CONFIG__</c>.
    /// </summary>
    /// <remarks>
    /// Anonymous because the JavaScript Injector loads it with a plain script tag.
    /// The response contains only overlay text and appearance settings; no user data is included.
    /// </remarks>
    /// <returns>The overlay script, or 404 when the embedded resource is missing.</returns>
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
}
