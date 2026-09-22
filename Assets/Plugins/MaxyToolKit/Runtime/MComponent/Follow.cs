using UnityEngine;

namespace MaxyToolKit.MComponent
{
    /// <summary>
    /// 让当前对象跟随目标对象的位置和旋转
    /// </summary>
    public sealed class Follow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private bool _followPosition = true;
        [SerializeField] private bool _followRotation;
        [SerializeField] private Vector3 _positionOffset;
        [SerializeField] private Vector3 _rotationOffset;
        [SerializeField] private float _positionSmoothTime;
        [SerializeField] private float _rotationSpeed = 12f;
        private Vector3 _velocity;

        /// <summary>
        /// 获取或设置跟随目标
        /// </summary>
        public Transform Target { get => _target; set => _target = value; }

        /// <summary>
        /// 在每帧结束时根据目标更新位置和旋转
        /// </summary>
        private void LateUpdate()
        {
            //目标不存在时不执行跟随
            if (_target == null) return;
            if (_followPosition)
            {
                //计算目标坐标系中的期望位置并按需平滑移动
                var desired = _target.TransformPoint(_positionOffset);
                transform.position = _positionSmoothTime <= 0f
                    ? desired
                    : Vector3.SmoothDamp(transform.position, desired, ref _velocity, _positionSmoothTime);
            }

            if (_followRotation)
            {
                //计算目标旋转叠加偏移并按需平滑旋转
                var desired = _target.rotation * Quaternion.Euler(_rotationOffset);
                transform.rotation = _rotationSpeed <= 0f
                    ? desired
                    : Quaternion.Slerp(transform.rotation, desired, _rotationSpeed * Time.deltaTime);
            }
        }
    }
}
