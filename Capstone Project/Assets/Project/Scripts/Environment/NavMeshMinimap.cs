using UnityEngine;
using UnityEngine.AI;

public class NavMeshMinimap : MonoBehaviour
{
    private static readonly int FootprintTexture = Shader.PropertyToID("_FootprintTexture");

    [Header("Minimap Settings")]
    [SerializeField] private Material minimapWireFrame;
    [SerializeField] private RenderTexture footprintRT;
    
    private GameObject _currentMinimapProxy;

    public void GenerateMinimap()
    {
        ClearOldMinimap();
        NavMeshTriangulation data = NavMesh.CalculateTriangulation();
        if(data.vertices.Length == 0) return;

        _currentMinimapProxy = new GameObject("NavMesh_Minimap_Proxy")
        {
            layer = LayerMask.NameToLayer("OverlayMap")
        };
        
        MeshFilter meshFilter = _currentMinimapProxy.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = _currentMinimapProxy.AddComponent<MeshRenderer>();
        
        Mesh wireframeMesh = new Mesh
        {
            vertices = data.vertices
        };
        
        int[] triangles = data.indices;
        int[] lines = new int[triangles.Length * 2];

        int index = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            lines[index++] = triangles[i]; lines[index++] = triangles[i + 1];
            lines[index++] = triangles[i + 1]; lines[index++] = triangles[i + 2];
            lines[index++] = triangles[i + 2]; lines[index++] = triangles[i];
        }
        
        wireframeMesh.SetIndices(lines, MeshTopology.Lines, 0);
        meshFilter.mesh = wireframeMesh;
        
        minimapWireFrame.SetTexture(FootprintTexture, footprintRT);
        meshRenderer.material = minimapWireFrame;

        _currentMinimapProxy.transform.position = new Vector3(0f, 0.05f, 0f);
    }

    public void ClearOldMinimap()
    {
        if(_currentMinimapProxy != null) Destroy(_currentMinimapProxy);
    }
}