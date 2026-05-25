using System;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Yuorself.Helpers;

public sealed class TransformPayload
{
    public string Contents { get; set; } = string.Empty;
}

public static class IndexHtmlPatch
{
    private const string BeginMarker = "<!-- BEGIN Yuorself Theme -->";
    private const string EndMarker = "<!-- END Yuorself Theme -->";

    public static string PatchIndexHtml(TransformPayload payload)
    {
        if (YuorselfPlugin.Instance?.Configuration.Enabled != true)
        {
            return payload.Contents;
        }

        string html = payload.Contents ?? string.Empty;

        html = RemoveExistingBlock(html);
        string injection = BuildInjectionBlock();

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

    private static string BuildInjectionBlock()
    {
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $@"
{BeginMarker}
<link rel=""stylesheet"" href=""./Yuorself/theme.css?v={ts}"" />
<script defer src=""./Yuorself/theme.js?v={ts}""></script>
{EndMarker}
";
    }
}
