using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Vòng cảnh báo trên mặt đất, TỰ DỰNG hình đĩa tròn bằng code nên không cần mesh hay material có sẵn.
    ///
    /// Cách làm prefab: tạo một GameObject rỗng, thêm component này, kéo xuống Project thành prefab,
    /// rồi gán vào ô "Telegraph Prefab" của AbilityDefinition. AbilityRunner tự đặt vị trí và scale
    /// (đường kính = 2 x bán kính của chiêu), nhấp nháy trong lúc windup và tự hủy khi đòn trúng hoặc bị ngắt.
    ///
    /// Dùng shader "Sprites/Default" (có trong mọi pipeline mặc định). Nếu dự án của bạn thay đổi
    /// Always Included Shaders, hãy chắc chắn shader này vẫn có mặt trong bản build.
    /// </summary>
    [DisallowMultipleComponent]
    public class TelegraphDisc : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 0.15f, 0.1f, 0.4f);
        [SerializeField, Min(0f)] private float pulseSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float pulseDepth = 0.5f;
        [SerializeField, Range(12, 64)] private int segments = 40;

        private Material _material;
        private Mesh _mesh;

        private void Awake()
        {
            _mesh = BuildDisc(segments);

            var filter = GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            _material = new Material(shader) { color = color };
            meshRenderer.sharedMaterial = _material;
        }

        private void Update()
        {
            if (_material == null) return;

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            Color c = color;
            c.a = color.a * (1f - pulseDepth + pulseDepth * wave);
            _material.color = c;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
            if (_mesh != null) Destroy(_mesh);
        }

        // Đĩa phẳng bán kính 0.5 trên mặt phẳng XZ, để AbilityRunner scale (2r, 1, 2r) ra đúng bán kính r.
        private static Mesh BuildDisc(int segmentCount)
        {
            var vertices = new Vector3[segmentCount + 1];
            var triangles = new int[segmentCount * 3];

            vertices[0] = Vector3.zero;
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = (float)i / segmentCount * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segmentCount + 1;
            }

            var mesh = new Mesh { name = "TelegraphDisc" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
