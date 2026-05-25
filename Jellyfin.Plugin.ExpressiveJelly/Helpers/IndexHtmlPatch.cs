using System;
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

    public static string PatchIndexHtml(TransformPayload payload)
    {
        if (ExpressiveJellyPlugin.Instance?.Configuration.Enabled != true)
        {
            return payload.Contents;
        }

        string html = payload.Contents ?? string.Empty;

        html = RemoveExistingBlock(html);
        bool dynamicEnabled = ExpressiveJellyPlugin.Instance?.Configuration.DynamicThemingEnabled != false;
        string injection = BuildInjectionBlock(dynamicEnabled);

        int headClose = html.LastIndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        if (headClose >= 0)
        {
            return html.Insert(headClose, injection);
        }

        int bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyClose >= 0)
        {
            return html.Insert(bodyClose, injection);
        }

        return html + injection;
    }

    private static string RemoveExistingBlock(string html)
    {
        string pattern = Regex.Escape(BeginMarker) + "[\\s\\S]*?" + Regex.Escape(EndMarker);
        return Regex.Replace(html, pattern, string.Empty, RegexOptions.IgnoreCase);
    }

    private static string BuildInjectionBlock(bool dynamicThemingEnabled)
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string dyn = dynamicThemingEnabled ? "1" : "0";
        return $@"
{BeginMarker}
<link rel=""stylesheet"" href=""../ExpressiveJelly/theme.css?v={ts}"" />
<script defer src=""../ExpressiveJelly/theme.js?dyn={dyn}&v={ts}""></script>
{EndMarker}
";
    }
}
