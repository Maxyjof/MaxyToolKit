// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using MaxyMCP.Editor.MCP.Server;
using MaxyMCP.Editor.Settings;
using MaxyMCP.Editor.Services.UnityLogs;
using UnityEditor;
using UnityEngine;

namespace MaxyMCP.Editor.DI
{
    [InitializeOnLoad]
    internal static class RootScopeServices
    {
        private static ServiceProvider _serviceProvider;

        public static IServiceProvider Services => _serviceProvider;

        static RootScopeServices()
        {
            if (Application.isBatchMode)
            {
                PluginDebugLogger.Log("[MaxyMCP] Root services skipped in Unity batch mode process.");
                return;
            }

            Initialize();
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void Initialize()
        {
            try
            {
                var services = new ServiceCollection();
                services.RegisterServices();
                _serviceProvider = services.BuildServiceProvider();
                PluginDebugLogger.Log("[MaxyMCP] Root services initialized.");

                var unityLogsRepository =
                    _serviceProvider.GetService(typeof(UnityLogsRepository)) as UnityLogsRepository;
                unityLogsRepository?.StartListening();

                var settings = _serviceProvider.GetService(typeof(ISettingsController)) as ISettingsController;
                if (settings != null)
                {
                    MaxyMCPClientConfigPanel.TryMigrateLegacyClaudeCodeEntryOnce(settings);
                }

                if (settings?.MCPServerEnabled == true &&
                    !MCPServerDomainReloadHandler.IsPendingPostReloadRestart())
                {
                    // Cold-start path only. During a domain reload, OnAfterReload owns the restart
                    // so the previous AppDomain's listener has time to release the port.
                    var mcpServer = _serviceProvider.GetService(typeof(MCPServerService)) as MCPServerService;
                    if (mcpServer != null)
                    {
                        _ = mcpServer.StartAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MaxyMCP] Failed to initialize root services: {ex}");
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            try
            {
                MCPServerDomainReloadHandler.PrepareForReload(_serviceProvider);
                _serviceProvider?.Dispose();
                _serviceProvider = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MaxyMCP] Error disposing root services: {ex}");
            }
        }
    }
}
