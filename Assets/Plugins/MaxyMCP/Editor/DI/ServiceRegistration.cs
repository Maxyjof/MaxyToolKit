// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using MaxyMCP.Editor.MCP.Server;
using MaxyMCP.Editor.Services;
using MaxyMCP.Editor.Services.UnityLogs;
using MaxyMCP.Editor.Settings;
using MaxyMCP.Editor.State;
using MaxyMCP.Editor.Threading;
using MaxyMCP.Editor.Tools;

namespace MaxyMCP.Editor.DI
{
    internal static class ServiceRegistration
    {
        public static ServiceCollection RegisterServices(this ServiceCollection services)
        {
            // Core Infrastructure (Singletons)
            services.AddSingleton<IApplicationPaths, ApplicationPaths>();
            services.AddSingleton<IEditorStateService, EditorStateService>();
            services.AddSingleton<IEditorContextBuilder, EditorContextBuilder>();
            services.AddSingleton<ISettingsController, SettingsController>();
            services.AddSingleton<IEditorThreadHelper, EditorThreadHelper>();

            // Services (Singletons)
            services.AddSingleton<ICompilationService, CompilationService>();
            services.AddSingleton<UnityLogsRepository, UnityLogsRepository>();
            services.AddSingleton<FunctionInvokerController, FunctionInvokerController>();

            // MCP Server (Singleton)
            services.AddSingleton<MCPServerService, MCPServerService>();

            // State (Scoped)
            services.AddScoped<IStateController, StateController>();

            return services;
        }
    }
}
