using UnityEngine;

namespace MaxyToolKit.MComponent
{
    /// <summary>
    /// 使用盒形范围检测场景中的Collider
    /// </summary>
    public sealed class BoxDetection : MonoBehaviour
    {
        [SerializeField] private Vector3 _halfExtents = Vector3.one * 0.5f;
        [SerializeField] private LayerMask _layerMask = ~0;
        [SerializeField] private QueryTriggerInteraction _triggers = QueryTriggerInteraction.Ignore;
        [SerializeField] private int _bufferSize = 32;
        private Collider[] _buffer;

        /// <summary>
        /// 将检测结果写入调用方提供的数组
        /// </summary>
        /// <param name="results">
        /// 用于接收Collider结果的数组
        /// </param>
        /// <returns>
        /// 实际检测到的Collider数量
        /// </returns>
        public int Detect(Collider[] results)
        {
            return Physics.OverlapBoxNonAlloc(transform.position, _halfExtents, results, transform.rotation, _layerMask, _triggers);
        }

        /// <summary>
        /// 使用内部缓冲区执行盒形检测
        /// </summary>
        /// <returns>
        /// 实际检测到的Collider数量
        /// </returns>
        public int Detect()
        {
            //根据配置准备可复用的结果缓冲区
            if (_buffer == null || _buffer.Length != Mathf.Max(1, _bufferSize)) _buffer = new Collider[Mathf.Max(1, _bufferSize)];
            return Detect(_buffer);
        }

        /// <summary>
        /// 在选中对象时绘制检测盒辅助线
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            //保存并替换Gizmos矩阵以匹配对象姿态
            Gizmos.color = Color.cyan;
            var matrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _halfExtents * 2f);
            //恢复绘制前的全局矩阵
            Gizmos.matrix = matrix;
        }
    }
}
