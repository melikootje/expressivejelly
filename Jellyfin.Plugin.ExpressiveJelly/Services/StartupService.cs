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

        _ = Task.Run(async () =>
        {
            await TryRegisterFileTransformationWithRetries(_logger, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task TryRegisterFileTransformationWithRetries(ILogger logger, CancellationToken cancellationToken)
    {
        // File Transformation may load after us; retry briefly so injection still registers.
        for (int attempt = 1; attempt <= 25 && !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                Assembly? fileTransformationAssembly = AssemblyLoadContext.All
                    .SelectMany(x => x.Assemblies)
                    .FirstOrDefault(x => (x.FullName ?? string.Empty).Contains(".FileTransformation", StringComparison.OrdinalIgnoreCase));

                if (fileTransformationAssembly == null)
                {
                    if (attempt == 1)
                    {
                        logger.LogWarning("File Transformation plugin not found yet; will retry.");
                    }
                }
                else
                {
                    Type? pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
                    MethodInfo? registerMethod = pluginInterfaceType?.GetMethod("RegisterTransformation");

                    if (registerMethod != null)
                    {
                        // Give other plugins (Moonfin, HomeScreenSections, etc.) a chance to register their
                        // own transformations first, so we end up at the tail of the pipeline.
                        if (attempt < 6)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                        }

                        // Use a fresh ID on each boot so we always register at the end of the pipeline.
                        // This avoids other plugins overwriting index.html after our transform.
                        JObject payload = new JObject
                        {
                            ["id"] = Guid.NewGuid(),
                            // Regex pattern (escape the dot) so it reliably matches index.html.
                            ["fileNamePattern"] = "index\\.html",
                            ["transformationEndpoint"] = string.Empty,
                            ["callbackAssembly"] = typeof(IndexHtmlPatch).Assembly.FullName,
                            ["callbackClass"] = typeof(IndexHtmlPatch).FullName,
                            ["callbackMethod"] = nameof(IndexHtmlPatch.PatchIndexHtml),
                        };

                        registerMethod.Invoke(null, new object?[] { payload });
                        logger.LogInformation("Registered index.html transformation for ExpressiveJelly web injection (pipeline tail).");
                        return;
                    }

                    if (attempt == 1)
                    {
                        logger.LogWarning("File Transformation PluginInterface is present but RegisterTransformation is missing; will retry.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Common if FileTransformationPlugin.Instance/ServiceProvider isn't ready yet.
                logger.LogWarning(ex, "ExpressiveJelly failed to register File Transformation (attempt {Attempt}/25); will retry.", attempt);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        logger.LogError("ExpressiveJelly could not register with File Transformation after multiple attempts; web injection will not run.");
    }
}
