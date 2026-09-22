// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

using System;
using System.Reflection;

namespace MaxyMCP.Editor.DI
{
    internal static class InjectionExtensions
    {
        public static void InjectDependencies(this IServiceProvider serviceProvider, object target)
        {
            if (serviceProvider == null || target == null) return;

            var type = target.GetType();
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var field in type.GetFields(bindingFlags))
            {
                if (field.GetCustomAttribute<InjectAttribute>() == null) continue;
                var service = serviceProvider.GetService(field.FieldType);
                if (service != null)
                    field.SetValue(target, service);
            }

            foreach (var property in type.GetProperties(bindingFlags))
            {
                if (property.GetCustomAttribute<InjectAttribute>() == null) continue;
                if (!property.CanWrite) continue;
                var service = serviceProvider.GetService(property.PropertyType);
                if (service != null)
                    property.SetValue(target, service);
            }
        }
    }
}
