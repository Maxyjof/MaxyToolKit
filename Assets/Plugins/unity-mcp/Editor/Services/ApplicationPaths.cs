// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System.IO;
using UnityEngine;

namespace MaxyMCP.Editor.Services
{
    internal class ApplicationPaths : IApplicationPaths
    {
        public string ProjectPath => Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        public string AssetsPath => Application.dataPath;
        public string TempPath => Path.Combine(ProjectPath, "Temp", "MaxyMCP");
        public string DataPath => AssetsPath;
        public string PersistentDataPath => Application.persistentDataPath;
    }
}
