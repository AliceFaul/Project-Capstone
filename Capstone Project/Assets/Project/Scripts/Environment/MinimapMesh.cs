using UnityEngine;
using UnityEngine.AI;

public class MinimapMesh : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;

    private void Awake()
    {
        if(meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        Bake();
    }

    [ContextMenu("Bake")]
    private void Bake()
    {
        if (meshFilter == null)
        {
            Debug.LogError($"[MinimapMesh] Need to assign MeshFilter.");
            return;
        }
        
        // Get NavMesh data
        var triangles = NavMesh.CalculateTriangulation();
        if (triangles.vertices.Length == 0)
        {
            Debug.LogWarning($"[MinimapMesh] No vertices found in NavMesh.");
            return;
        }
        
        Vector3[] localVertices = new Vector3[triangles.vertices.Length];
        for (int i = 0; i < triangles.vertices.Length; i++)
        {
            localVertices[i] = transform.InverseTransformPoint(triangles.vertices[i]);
        }
        
        // Create new mesh with data
        Mesh mesh = new Mesh
        {
            vertices = localVertices,
            triangles = triangles.indices
        };
        
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        
        Debug.Log($"[MinimapMesh] Baked mesh minimap successfully!");
    }
}