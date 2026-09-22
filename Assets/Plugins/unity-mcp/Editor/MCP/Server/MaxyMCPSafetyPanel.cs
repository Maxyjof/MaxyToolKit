// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using MaxyMCP.Editor.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyMCP.Editor.MCP.Server
{
    internal static class MaxyMCPSafetyPanel
    {
        public static void AddTo(VisualElement parent, ISettingsController settings)
        {
            if (parent == null || settings == null)
                return;

            var header = new Label(MaxyMCPLocalization.T("Safety"));
            header.style.fontSize = 12;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.75f, 0.75f, 0.75f);
            header.style.marginBottom = 4;
            parent.Add(header);

            var safetyToggle = new Toggle(MaxyMCPLocalization.T("Default execute_code safety checks"));
            safetyToggle.SetValueWithoutNotify(settings.ExecuteCodeSafetyChecksEnabled);
            safetyToggle.RegisterValueChangedCallback(evt =>
            {
                settings.ExecuteCodeSafetyChecksEnabled = evt.newValue;
            });
            safetyToggle.style.marginBottom = 2;
            parent.Add(safetyToggle);
            AddHint(parent,
                MaxyMCPLocalization.T("Default for execute_code calls when safety_checks is omitted. Explicit safety_checks=false can still bypass this for trusted local calls."));

            var strictToggle = new Toggle(MaxyMCPLocalization.T("Strict filesystem guard"));
            strictToggle.SetValueWithoutNotify(settings.ExecuteCodeStrictFilesystemSafetyEnabled);
            strictToggle.RegisterValueChangedCallback(evt =>
            {
                settings.ExecuteCodeStrictFilesystemSafetyEnabled = evt.newValue;
            });
            strictToggle.style.marginBottom = 2;
            parent.Add(strictToggle);
            AddHint(parent,
                MaxyMCPLocalization.T("Adds checks for broad System.IO file writes, raw file streams, and absolute/user/system/traversal paths. This is a defensive guard, not a complete sandbox."));

            var projectNamespacesToggle = new Toggle(MaxyMCPLocalization.T("Auto-inject project namespaces"));
            projectNamespacesToggle.SetValueWithoutNotify(settings.ExecuteCodeProjectNamespaceInjectionEnabled);
            projectNamespacesToggle.RegisterValueChangedCallback(evt =>
            {
                settings.ExecuteCodeProjectNamespaceInjectionEnabled = evt.newValue;
            });
            projectNamespacesToggle.style.marginBottom = 2;
            parent.Add(projectNamespacesToggle);
            AddHint(parent,
                MaxyMCPLocalization.T("Off by default. When enabled, only namespaces from loaded Library/ScriptAssemblies assemblies are injected; explicit using directives remain the least ambiguous option."));
        }

        private static void AddHint(VisualElement parent, string text)
        {
            var hint = new Label(text);
            hint.style.fontSize = 10;
            hint.style.color = new Color(0.65f, 0.65f, 0.65f);
            hint.style.marginLeft = 18;
            hint.style.marginBottom = 8;
            hint.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(hint);
        }
    }
}
