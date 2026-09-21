// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

namespace MaxyMCP.Editor.Services
{
    internal interface IApplicationPaths
    {
        string ProjectPath { get; }
        string AssetsPath { get; }
        string TempPath { get; }
        string DataPath { get; }
        string PersistentDataPath { get; }
    }
}
