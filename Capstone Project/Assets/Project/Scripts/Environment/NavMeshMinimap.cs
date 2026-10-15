using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class NavMeshMinimap : MonoBehaviour
{
    private static readonly int FootprintTexture = Shader.PropertyToID("_FootprintTexture");

    [Header("Minimap Settings")]
    [SerializeField] private Material minimapWireFrame;
    [SerializeField] private RenderTexture footprintRT;
    [SerializeField] private Transform playerTransform;
    
    private GameObject _currentMinimapProxy;
    private Material _instancedMaterial;
    private bool _isMinimapVisible = true;

    private void Start()
    {
        if(playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        GenerateMinimap();
    }

    private void Update()
    {
        if(Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) ToggleMinimap();
    }

    public void ToggleMinimap()
    {
        if(_currentMinimapProxy == null) return;
        
        _isMinimapVisible = !_isMinimapVisible;
        _currentMinimapProxy.SetActive(_isMinimapVisible);
        Debug.Log($"[NavMeshMinimap] Minimap Visible: {_isMinimapVisible}");
    }

    public void GenerateMinimap()
    {
        ClearOldMinimap();
        NavMeshTriangulation data = NavMesh.CalculateTriangulation();
        if(data.vertices.Length == 0) return;
        
        int layerIndex = LayerMask.NameToLayer("OverlayMap");
        if(layerIndex == -1) Debug.LogWarning($"[NavMeshMinimap] Need to create 'OverlayMap' layer in Project Settings.");

        _currentMinimapProxy = new GameObject("NavMesh_Minimap_Proxy")
        {
            layer = layerIndex != -1 ? layerIndex : 0,
            transform =
            {
                position = new Vector3(0f, 0.05f, 0f)
            }
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

        // Create a material instance to avoid affecting the original asset.
        if (minimapWireFrame != null)
        {
            _instancedMaterial = new Material(minimapWireFrame);
            _instancedMaterial.SetTexture(FootprintTexture, footprintRT);
            meshRenderer.material = _instancedMaterial;
        }
        
        _currentMinimapProxy.SetActive(_isMinimapVisible);
    }

    public void ClearOldMinimap()
    {
        if (_instancedMaterial != null)
        {
            Destroy(_instancedMaterial);
            _instancedMaterial = null;
        }
        
        if(_currentMinimapProxy != null) Destroy(_currentMinimapProxy);
    }
}