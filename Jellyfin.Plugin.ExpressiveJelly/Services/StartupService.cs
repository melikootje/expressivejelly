using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExpressiveJelly.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.ExpressiveJelly.Services;

public sealed class StartupService : IHostedService
{
    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (ExpressiveJellyPlugin.Instance?.Configuration.Enabled != true)
        {
            _logger.LogInformation("ExpressiveJelly is disabled; skipping web injection registration.");
            return Task.CompletedTask;
        }

        TryRegisterFileTransformation(_logger);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static void TryRegisterFileTransformation(ILogger logger)
    {
        try
        {
            Assembly? fileTransformationAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => (x.FullName ?? string.Empty).Contains("Jellyfin.Plugin.FileTransformation", StringComparison.OrdinalIgnoreCase));

            if (fileTransformationAssembly == null)
            {
                logger.LogWarning("File Transformation plugin not found; theme injection will not run.");
                return;
            }

            Type? pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            if (pluginInterfaceType == null)
            {
                logger.LogWarning("File Transformation PluginInterface type not found; theme injection will not run.");
                return;
            }

            MethodInfo? registerMethod = pluginInterfaceType.GetMethod("RegisterTransformation");
            if (registerMethod == null)
            {
                logger.LogWarning("File Transformation RegisterTransformation method not found; theme injection will not run.");
                return;
            }

            JObject payload = new JObject
            {
                ["id"] = ExpressiveJellyPlugin.Instance!.Id,
                ["fileNamePattern"] = "index.html",
                ["transformationEndpoint"] = "/",
                ["callbackAssembly"] = typeof(IndexHtmlPatch).Assembly.FullName,
                ["callbackClass"] = typeof(IndexHtmlPatch).FullName,
                ["callbackMethod"] = nameof(IndexHtmlPatch.PatchIndexHtml),
            };

            registerMethod.Invoke(null, new object?[] { payload });
            logger.LogInformation("Registered index.html transformation for ExpressiveJelly web injection.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register File Transformation for ExpressiveJelly.");
        }
    }
}
