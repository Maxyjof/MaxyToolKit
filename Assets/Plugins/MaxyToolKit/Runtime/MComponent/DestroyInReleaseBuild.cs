using UnityEngine;

namespace MaxyToolKit.MComponent
{
    /// <summary>
    /// 仅在正式发布包运行时销毁自身
    /// </summary>
    public sealed class DestroyInReleaseBuild : MonoBehaviour
    {
        /// <summary>
        /// 在对象启用时检查当前构建类型并销毁对象
        /// </summary>
        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(gameObject);
#endif
        }
    }
}
