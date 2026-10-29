using UnityEngine;
using UnityEngine.AI;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Đặt Avoidance Priority ngẫu nhiên trong khoảng [min, max] cho mỗi enemy khi sinh ra.
    /// Các enemy có cùng priority thì xô đẩy nhau và dễ kẹt; khác priority thì con ưu tiên thấp nhường đường.
    /// Gắn cho quái thường (50-60). KHÔNG gắn cho boss: giữ priority cố định (ví dụ 40) trên EnemyMover.
    /// Player nên có priority thấp nhất (ví dụ 30) để enemy không đẩy lệch đường click.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public class AvoidancePriorityJitter : MonoBehaviour
    {
        [SerializeField, Range(0, 99)] private int minPriority = 50;
        [SerializeField, Range(0, 99)] private int maxPriority = 60;

        private void Start()
        {
            // Start chạy sau Awake của EnemyMover (nơi đặt priority mặc định) nên giá trị này được giữ.
            var agent = GetComponent<NavMeshAgent>();
            int low = Mathf.Min(minPriority, maxPriority);
            int high = Mathf.Max(minPriority, maxPriority);
            agent.avoidancePriority = Random.Range(low, high + 1);
        }
    }
}
