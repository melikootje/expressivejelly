using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ExpressiveJelly.Controllers;

[ApiController]
[Route("ExpressiveJelly")]
public sealed class ThemeController : ControllerBase
{
    [HttpGet("theme.css")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult GetCss()
    {
        if (ExpressiveJellyPlugin.Instance?.Configuration.Enabled != true)
        {
            return NotFound();
        }

        string css = ReadEmbeddedText("Jellyfin.Plugin.ExpressiveJelly.Resources.jellyfinexpressive.css");
        return Content(css, "text/css; charset=utf-8");
    }

    [HttpGet("theme.js")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult GetJs()
    {
        if (ExpressiveJellyPlugin.Instance?.Configuration.Enabled != true)
        {
            return NotFound();
        }

        string js = ReadEmbeddedText("Jellyfin.Plugin.ExpressiveJelly.Resources.jellyfinexpressive.js");
        return Content(js, "application/javascript; charset=utf-8");
    }

    private static string ReadEmbeddedText(string resourceName)
    {
        Assembly asm = typeof(ThemeController).Assembly;
        using Stream? s = asm.GetManifestResourceStream(resourceName);
        if (s == null)
        {
            throw new InvalidOperationException($"Missing embedded resource: {resourceName}");
        }

        using StreamReader r = new StreamReader(s);
        return r.ReadToEnd();
    }
}
