using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaxyToolKit.Tool
{
    /// <summary>
    /// 提供Unity对象、输入、角度、物理和集合相关的常用工具
    /// </summary>
    public static class MTool
    {
        /// <summary>
        /// 获取组件，不存在时自动添加
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="gameObject">目标GameObject</param>
        /// <returns>已有或新添加的组件，目标为空时返回空</returns>
        public static T GetOrAdd<T>(this GameObject gameObject) where T : UnityEngine.Component
        {
            if (gameObject == null) return null;
            if (gameObject.TryGetComponent<T>(out var component)) return component;
            return gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 销毁父对象下的全部子对象
        /// </summary>
        /// <param name="parent">子对象所属的Transform</param>
        public static void DestroyChildren(this Transform parent)
        {
            if (parent == null) return;
            //倒序销毁以避免层级索引变化
            for (var index = parent.childCount - 1; index >= 0; index--) UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
        }

        /// <summary>
        /// 重置Transform的本地位置、旋转和缩放
        /// </summary>
        /// <param name="transform">要重置的Transform</param>
        public static void ResetLocal(this Transform transform)
        {
            if (transform == null) return;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 递归设置GameObject及全部子对象的层
        /// </summary>
        /// <param name="gameObject">要设置的根GameObject</param>
        /// <param name="layer">目标层索引</param>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (gameObject == null) return;
            //先设置根对象，再递归处理子对象
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform) child.gameObject.SetLayerRecursively(layer);
        }

        /// <summary>
        /// 从列表中随机取一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">目标列表</param>
        /// <param name="fallback">列表为空时返回的默认值</param>
        /// <returns>随机元素或默认值</returns>
        public static T RandomOrDefault<T>(this IList<T> list, T fallback = default)
        {
            return list == null || list.Count == 0 ? fallback : list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 使用Fisher-Yates算法随机打乱列表
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">要打乱的列表</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            //从尾部向前交换随机元素
            for (var index = list.Count - 1; index > 0; index--)
            {
                var randomIndex = UnityEngine.Random.Range(0, index + 1);
                (list[index], list[randomIndex]) = (list[randomIndex], list[index]);
            }
        }

        /// <summary>
        /// 将数值从一个范围重新映射到另一个范围
        /// </summary>
        /// <param name="value">待映射的数值</param>
        /// <param name="fromMin">原范围最小值</param>
        /// <param name="fromMax">原范围最大值</param>
        /// <param name="toMin">目标范围最小值</param>
        /// <param name="toMax">目标范围最大值</param>
        /// <returns>映射后的数值</returns>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            //原范围无长度时直接返回目标范围起点
            if (Mathf.Approximately(fromMin, fromMax)) return toMin;
            return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
        }

        /// <summary>
        /// 将音量比例转换为分贝
        /// </summary>
        /// <param name="value">音量比例</param>
        /// <returns>对应的分贝值</returns>
        public static float ToDB(this float value)
        {
            return Mathf.Log10(Mathf.Clamp(value, 0.0001f, 10f)) * 20f;
        }

        /// <summary>
        /// 将0到360范围的角度转换到-180到180范围
        /// </summary>
        /// <param name="angle">待转换的角度</param>
        /// <returns>转换后的角度</returns>
        public static float TranslateAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// 将角度规范到0到360范围
        /// </summary>
        /// <param name="angle">待规范的角度</param>
        /// <returns>规范后的角度</returns>
        public static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// 获取移动输入
        /// </summary>
        /// <param name="onlyWASD">是否只读取WASD按键</param>
        /// <returns>未平滑的二维移动输入</returns>
        public static Vector2 GetMoveInput(bool onlyWASD = true)
        {
            if (!onlyWASD) return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var x = (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0);
            var y = (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0);
            return new Vector2(x, y);
        }

        /// <summary>
        /// 获取Unity输入系统提供的平滑移动输入
        /// </summary>
        /// <returns>平滑的二维移动输入</returns>
        public static Vector2 GetMoveInputSmoothly()
        {
            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        /// <summary>
        /// 显示系统鼠标光标
        /// </summary>
        public static void ShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// 锁定并隐藏系统鼠标光标
        /// </summary>
        public static void HideCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// 设置Unity目标帧率
        /// </summary>
        /// <param name="frameRate">目标帧率，传入小于等于0时使用平台默认值</param>
        public static void SetTargetFrameRate(int frameRate)
        {
            Application.targetFrameRate = frameRate;
        }

        /// <summary>
        /// 将二维方向转换为英文方向名称
        /// </summary>
        /// <param name="direction">二维方向</param>
        /// <param name="preferX">对角线方向相等时是否优先使用水平方向</param>
        /// <returns>Down、Up、Left或Right</returns>
        public static string TranslateDirectionToEngString(Vector2 direction, bool preferX = false)
        {
            if (direction == Vector2.zero) return "Down";
            if (preferX && Mathf.Approximately(Mathf.Abs(direction.x), Mathf.Abs(direction.y)))
            {
                if (direction.x > 0f) return "Right";
                if (direction.x < 0f) return "Left";
            }
            //Y轴绝对值更大时优先判断垂直方向
            if (Mathf.Abs(direction.x) < Mathf.Abs(direction.y)) return direction.y > 0f ? "Up" : "Down";
            return direction.x > 0f ? "Right" : "Left";
        }

        /// <summary>
        /// 让对象的前方朝向二维目标
        /// </summary>
        /// <param name="self">需要旋转的Transform</param>
        /// <param name="target">目标世界坐标</param>
        public static void LookAt2D(Transform self, Vector3 target)
        {
            if (self == null) return;
            var direction = target - self.position;
            direction.z = 0f;
            if (direction.sqrMagnitude < 0.000001f) return;
            var targetAngle = NormalizeAngle(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            var euler = self.eulerAngles;
            self.rotation = Quaternion.Euler(euler.x, euler.y, targetAngle);
        }

        /// <summary>
        /// 平滑让对象的前方朝向二维目标
        /// </summary>
        /// <param name="self">需要旋转的Transform</param>
        /// <param name="target">目标世界坐标</param>
        /// <param name="smoothRate">每秒旋转速度</param>
        /// <returns>是否已经基本对准目标</returns>
        public static bool LookAt2D(Transform self, Vector3 target, float smoothRate)
        {
            if (self == null) return false;
            var direction = target - self.position;
            direction.z = 0f;
            if (direction.sqrMagnitude < 0.000001f) return true;
            var targetAngle = NormalizeAngle(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            var euler = self.eulerAngles;
            var targetRotation = Quaternion.Euler(euler.x, euler.y, targetAngle);
            //使用四元数角度判断以正确处理0度和360度的边界
            if (Quaternion.Angle(self.rotation, targetRotation) > 1f)
            {
                self.rotation = Quaternion.RotateTowards(self.rotation, targetRotation, Mathf.Max(0f, smoothRate) * Time.deltaTime * 100f);
                return false;
            }
            self.rotation = targetRotation;
            return true;
        }

        /// <summary>
        /// 只通过改变本地X缩放让二维对象朝向目标
        /// </summary>
        /// <param name="self">需要翻转的Transform</param>
        /// <param name="target">目标Transform</param>
        public static void LookAt2DOnlyX(Transform self, Transform target)
        {
            if (self == null || target == null) return;
            var direction = target.position - self.position;
            direction.z = 0f;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var scale = self.localScale;
            scale.x = angle > 90f || angle < -90f ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            self.localScale = scale;
        }

        /// <summary>
        /// 检测二维圆形范围内的第一个对象
        /// </summary>
        /// <param name="position">圆心位置</param>
        /// <param name="range">检测半径</param>
        /// <param name="layerMask">参与检测的层掩码</param>
        /// <returns>检测到的对象，未检测到时返回空</returns>
        public static Transform DetectObjectCircle2D(Vector2 position, float range, int layerMask)
        {
            var collider = Physics2D.OverlapCircle(position, Mathf.Max(0f, range), layerMask);
            return collider == null ? null : collider.transform;
        }

        /// <summary>
        /// 计算射线与Y等于指定高度的水平面的交点
        /// </summary>
        /// <param name="ray">待计算的射线</param>
        /// <param name="height">水平面高度</param>
        /// <param name="hitPoint">输出交点</param>
        /// <returns>射线前方存在交点时返回真</returns>
        public static bool GetRayPlaneIntersection(Ray ray, float height, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            if (Mathf.Approximately(ray.direction.y, 0f)) return false;
            var distance = (height - ray.origin.y) / ray.direction.y;
            if (distance < 0f) return false;
            hitPoint = ray.origin + ray.direction * distance;
            return true;
        }

        /// <summary>
        /// 计算二维三阶贝塞尔曲线的中点
        /// </summary>
        /// <param name="self">曲线起点对象，自身up方向视为前方</param>
        /// <param name="targetPosition">曲线终点位置</param>
        /// <param name="curveStrength">曲线弯曲强度</param>
        /// <returns>曲线在中间时刻的位置</returns>
        public static Vector3 CalculateBezierPoint2D(Transform self, Vector3 targetPosition, float curveStrength = 3f)
        {
            if (self == null) return targetPosition;
            var start = self.position;
            var toTarget = targetPosition - start;
            var forward = self.up;
            var right = new Vector3(-forward.y, forward.x, 0f);
            var side = Mathf.Sign(Vector3.Dot(toTarget, right));
            if (Mathf.Approximately(side, 0f)) side = 1f;
            var finalRight = right * side;
            //根据起点前方和终点位置构造两个控制点
            var controlPoint1 = start + finalRight * curveStrength;
            var controlPoint2 = targetPosition - forward * curveStrength;
            const float time = 0.5f;
            var inverseTime = 1f - time;
            return inverseTime * inverseTime * inverseTime * start
                   + 3f * inverseTime * inverseTime * time * controlPoint1
                   + 3f * inverseTime * time * time * controlPoint2
                   + time * time * time * targetPosition;
        }

        /// <summary>
        /// 计算二维抛物线的初速度
        /// </summary>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <param name="apexHeight">相对起点的顶点高度</param>
        /// <param name="interferencePercent">水平速度随机干扰比例</param>
        /// <param name="gravity">重力加速度</param>
        /// <returns>初速度向量</returns>
        public static Vector2 CalculateVelocityOfParabola2D(Vector2 startPoint, Vector2 endPoint, float apexHeight, float interferencePercent = 0f, float gravity = 9.8f)
        {
            gravity = Mathf.Max(0.0001f, Mathf.Abs(gravity));
            apexHeight = Mathf.Max(0.01f, apexHeight);
            interferencePercent = Mathf.Clamp01(interferencePercent);
            var deltaX = endPoint.x - startPoint.x;
            var deltaY = endPoint.y - startPoint.y;
            //水平距离接近零时只计算垂直上抛速度
            if (Mathf.Abs(deltaX) < 0.001f) return CalculateVerticalVelocity(apexHeight, gravity);
            var verticalVelocity = Mathf.Sqrt(2f * gravity * apexHeight);
            var timeToApex = verticalVelocity / gravity;
            var fallDistance = Mathf.Max(0.1f, apexHeight - deltaY);
            var timeFromApexToEnd = Mathf.Sqrt(2f * fallDistance / gravity);
            var totalTime = timeToApex + timeFromApexToEnd;
            var horizontalVelocity = deltaX / totalTime;
            return new Vector2(GetRandomWithPercent(horizontalVelocity, interferencePercent), verticalVelocity);
        }

        /// <summary>
        /// 加载指定名称的场景
        /// </summary>
        /// <param name="sceneName">场景名称或场景路径</param>
        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return;
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 计算垂直上抛所需的初速度
        /// </summary>
        /// <param name="apexHeight">顶点高度</param>
        /// <param name="gravity">重力加速度</param>
        /// <returns>垂直初速度</returns>
        private static Vector2 CalculateVerticalVelocity(float apexHeight, float gravity)
        {
            return new Vector2(0f, Mathf.Sqrt(2f * gravity * apexHeight));
        }

        /// <summary>
        /// 按比例为数值添加随机干扰
        /// </summary>
        /// <param name="value">原始数值</param>
        /// <param name="percent">干扰比例</param>
        /// <returns>添加干扰后的数值</returns>
        private static float GetRandomWithPercent(float value, float percent)
        {
            if (percent <= 0f || Mathf.Approximately(value, 0f)) return value;
            var maxOffset = Mathf.Abs(value) * percent;
            return value + UnityEngine.Random.Range(-maxOffset, maxOffset);
        }
    }
}
