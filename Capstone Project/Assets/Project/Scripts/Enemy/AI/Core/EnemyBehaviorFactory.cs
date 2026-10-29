using UnityEngine;

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;

    /// <summary>
    /// Dựng cây hành vi cho một loại enemy. Mỗi enemy gọi Build() riêng để có cây riêng
    /// (node giữ trạng thái nên không dùng chung được). Factory không giữ trạng thái.
    /// Thêm loại enemy mới = viết thêm một factory, không sửa EnemyBrain.
    /// </summary>
    public abstract class EnemyBehaviorFactory : ScriptableObject
    {
        public abstract Node Build(EnemyAIConfig config);
    }
}
