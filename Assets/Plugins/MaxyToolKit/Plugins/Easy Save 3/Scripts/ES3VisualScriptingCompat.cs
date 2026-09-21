#if UNITY_VISUAL_SCRIPTING
namespace Unity.VisualScripting
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public sealed class IncludeInSettingsAttribute : System.Attribute
    {
        public IncludeInSettingsAttribute(bool include)
        {
        }
    }
}
#endif
