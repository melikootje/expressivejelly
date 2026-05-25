using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.ExpressiveJelly.Helpers;

public sealed class TransformPayload
{
    public string Contents { get; set; } = string.Empty;
}

public static class IndexHtmlPatch
{
    private const string BeginMarker = "<!-- BEGIN ExpressiveJelly Theme -->";
    private const string EndMarker = "<!-- END ExpressiveJelly Theme -->";
    private static readonly string DebugLogPath = Path.Combine(Path.GetTempPath(), "expressivejelly-patch.log");

    private static readonly Lazy<string> ThemeCss = new(() => ReadEmbeddedText("Jellyfin.Plugin.ExpressiveJelly.Resources.jellyfinexpressive.css"));
    private static readonly Lazy<string> ThemeJs = new(() => ReadEmbeddedText("Jellyfin.Plugin.ExpressiveJelly.Resources.jellyfinexpressive.js"));

    public static string PatchIndexHtml(TransformPayload payload)
    {
        try
        {
            WriteDebug("PatchIndexHtml invoked.");

            if (payload == null)
            {
                WriteDebug("Payload was null.");
                return string.Empty;
            }

            if (ExpressiveJellyPlugin.Instance?.Configuration.Enabled != true)
            {
                WriteDebug("Plugin disabled. Returning original contents.");
                return payload.Contents;
            }

            string html = payload.Contents ?? string.Empty;
            WriteDebug($"Incoming HTML length: {html.Length}");

            html = RemoveExistingBlock(html);
            bool dynamicEnabled = ExpressiveJellyPlugin.Instance?.Configuration.DynamicThemingEnabled != false;
            string injection = BuildInjectionBlock(dynamicEnabled);
            WriteDebug($"Injection length: {injection.Length}");

            int headClose = html.LastIndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (headClose >= 0)
            {
                WriteDebug("Inserted before </head>.");
                return html.Insert(headClose, injection);
            }

            int bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyClose >= 0)
            {
                WriteDebug("Inserted before </body>.");
                return html.Insert(bodyClose, injection);
            }

            WriteDebug("No </head> or </body> found. Appending injection.");
            return html + injection;
        }
        catch (Exception ex)
        {
            WriteDebug($"PatchIndexHtml failed: {ex}");
            return payload?.Contents ?? string.Empty;
        }
    }

    private static string RemoveExistingBlock(string html)
    {
        string pattern = Regex.Escape(BeginMarker) + "[\\s\\S]*?" + Regex.Escape(EndMarker);
        return Regex.Replace(html, pattern, string.Empty, RegexOptions.IgnoreCase);
    }

    private static string BuildInjectionBlock(bool dynamicThemingEnabled)
    {
        string dyn = dynamicThemingEnabled ? "true" : "false";
        return $@"
{BeginMarker}
<style id=""expressivejelly-theme"">
{ThemeCss.Value}
</style>
<script>
window.__ExpressiveJelly = window.__ExpressiveJelly || {{}};
window.__ExpressiveJelly.dynamicThemingEnabled = {dyn};
</script>
<script id=""expressivejelly-theme-js"">
{ThemeJs.Value}
</script>
{EndMarker}
";
    }

    private static string ReadEmbeddedText(string resourceName)
    {
        Assembly asm = typeof(IndexHtmlPatch).Assembly;
        using Stream? s = asm.GetManifestResourceStream(resourceName);
        if (s == null)
        {
            return string.Empty;
        }

        using StreamReader r = new StreamReader(s);
        return r.ReadToEnd();
    }

    private static void WriteDebug(string message)
    {
        try
        {
            File.AppendAllText(DebugLogPath, $"[{DateTimeOffset.UtcNow:O}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}
