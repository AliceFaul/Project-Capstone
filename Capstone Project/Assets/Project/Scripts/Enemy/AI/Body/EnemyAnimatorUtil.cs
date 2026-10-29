using UnityEngine;

namespace Mhieu.Enemy
{
    internal static class EnemyAnimatorUtil
    {
        /// <summary>
        /// Kiểm tra animator có parameter đó không. Gọi MỘT LẦN ở Awake rồi cache kết quả,
        /// để enemy chưa có Animator Controller vẫn chạy mà không spam cảnh báo.
        /// </summary>
        public static bool HasParameter(Animator animator, int nameHash, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;

            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == nameHash && parameter.type == type) return true;
            }

            return false;
        }
    }
}
