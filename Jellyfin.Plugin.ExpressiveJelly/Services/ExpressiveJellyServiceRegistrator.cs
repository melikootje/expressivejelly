using Jellyfin.Plugin.ExpressiveJelly.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExpressiveJelly;

public sealed class ExpressiveJellyServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost serverApplicationHost)
    {
        serviceCollection.AddHostedService<StartupService>();
    }
}
