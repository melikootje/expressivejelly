using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Yuorself.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;
}

