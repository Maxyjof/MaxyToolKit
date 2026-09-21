using UnityEngine;

namespace MaxyToolKit
{
    /// <summary>
    /// 让当前对象跟随目标对象的位置和旋转
    /// </summary>
    public sealed class Follow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private bool followPosition = true;
        [SerializeField] private bool followRotation;
        [SerializeField] private Vector3 positionOffset;
        [SerializeField] private Vector3 rotationOffset;
        [SerializeField] private float positionSmoothTime;
        [SerializeField] private float rotationSpeed = 12f;
        private Vector3 velocity;

        /// <summary>
        /// 获取或设置跟随目标
        /// </summary>
        public Transform Target { get => target; set => target = value; }

        /// <summary>
        /// 在每帧结束时根据目标更新位置和旋转
        /// </summary>
        private void LateUpdate()
        {
            //目标不存在时不执行跟随
            if (target == null) return;
            if (followPosition)
            {
                //计算目标坐标系中的期望位置并按需平滑移动
                var desired = target.TransformPoint(positionOffset);
                transform.position = positionSmoothTime <= 0f
                    ? desired
                    : Vector3.SmoothDamp(transform.position, desired, ref velocity, positionSmoothTime);
            }

            if (followRotation)
            {
                //计算目标旋转叠加偏移并按需平滑旋转
                var desired = target.rotation * Quaternion.Euler(rotationOffset);
                transform.rotation = rotationSpeed <= 0f
                    ? desired
                    : Quaternion.Slerp(transform.rotation, desired, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
