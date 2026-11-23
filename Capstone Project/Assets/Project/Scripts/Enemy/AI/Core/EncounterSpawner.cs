using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Sinh một nhóm enemy quanh vị trí của object này (để test và để làm encounter).
    /// Mỗi enemy được đặt tại một điểm ngẫu nhiên trên NavMesh, cách nhau tối thiểu minSpacing.
    /// Vị trí sinh ra chính là "nhà" của enemy (EnemyBrain tự chốt ở frame đầu).
    ///
    /// Dùng: đặt object này cách player xa hơn Aggro Radius (ví dụ 12-15m), điền Entries, bấm Play.
    /// Cũng có thể chuột phải component trong Inspector: "Spawn Now" / "Clear Spawned".
    /// </summary>
    public class EncounterSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct Entry
        {
            public GameObject prefab;
            [Min(1)] public int count;
        }

        [SerializeField] private Entry[] entries;
        [SerializeField, Min(1f)] private float spawnRadius = 5f;
        [SerializeField, Min(0.5f)] private float minSpacing = 1.5f;
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public IReadOnlyList<GameObject> Spawned => _spawned;

        private void Start()
        {
            if (spawnOnStart) Spawn();
        }

        [ContextMenu("Spawn Now")]
        public void Spawn()
        {
            if (entries == null) return;

            var placed = new List<Vector3>();

            foreach (Entry entry in entries)
            {
                if (entry.prefab == null) continue;

                for (int i = 0; i < Mathf.Max(1, entry.count); i++)
                {
                    if (!TryFindPoint(placed, out Vector3 point))
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[EncounterSpawner] Không tìm được chỗ trên NavMesh cho '{entry.prefab.name}'. " +
                            "Kiểm tra NavMesh đã bake quanh đây chưa, hoặc tăng Spawn Radius.", this);
                        continue;
                    }

                    placed.Add(point);
                    GameObject instance = Instantiate(entry.prefab, point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    _spawned.Add(instance);
                }
            }
        }

        [ContextMenu("Clear Spawned")]
        public void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }

            _spawned.Clear();
        }

        private bool TryFindPoint(List<Vector3> placed, out Vector3 result)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * spawnRadius;
                Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;

                bool tooClose = false;
                for (int i = 0; i < placed.Count; i++)
                {
                    if (AIMath.FlatDistance(placed[i], hit.position) < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                result = hit.position;
                return true;
            }

            result = transform.position;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
