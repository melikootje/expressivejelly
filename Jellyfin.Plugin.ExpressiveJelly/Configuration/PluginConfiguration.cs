using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ExpressiveJelly.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;

    public bool DynamicThemingEnabled { get; set; } = true;
}
