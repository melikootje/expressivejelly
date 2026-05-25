using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Yuorself.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Yuorself;

public sealed class YuorselfPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static YuorselfPlugin? Instance { get; private set; }

    public YuorselfPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "Yuorself";

    public override Guid Id => new("2c76c109-0d9b-4375-80fe-522706044e39");

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
            },
        };
    }
}
