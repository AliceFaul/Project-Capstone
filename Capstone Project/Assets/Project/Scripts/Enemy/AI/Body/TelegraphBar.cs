using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Vạch cảnh báo hình chữ nhật trên mặt đất, TỰ DỰNG bằng code (không cần mesh/material).
    /// Mặt phẳng XZ, đi từ gốc (z = 0) dọc theo +Z tới z = 1, rộng 1 theo X;
    /// ChargeRunner scale (rộng, 1, dài) và xoay cho đúng đường lao.
    ///
    /// Cách làm prefab: tạo GameObject rỗng, thêm component này, kéo xuống Project thành prefab,
    /// gán vào ô "Telegraph Prefab" của ChargeDefinition.
    ///
    /// Khi hướng bị khóa, ChargeRunner gọi SetLocked(true): vạch đổi sang màu "locked" và hết nhấp nháy.
    /// Dùng shader "Sprites/Default".
    /// </summary>
    [DisallowMultipleComponent]
    public class TelegraphBar : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 0.6f, 0.1f, 0.35f);
        [SerializeField] private Color lockedColor = new Color(1f, 0.1f, 0.05f, 0.55f);
        [SerializeField, Min(0f)] private float pulseSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float pulseDepth = 0.4f;

        private Material _material;
        private Mesh _mesh;
        private bool _locked;

        public void SetLocked(bool locked) => _locked = locked;

        private void Awake()
        {
            _mesh = BuildBar();

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

            if (_locked)
            {
                _material.color = lockedColor;
                return;
            }

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

        private static Mesh BuildBar()
        {
            var mesh = new Mesh { name = "TelegraphBar" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(-0.5f, 0f, 1f),
                new Vector3(0.5f, 0f, 1f),
                new Vector3(0.5f, 0f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
