using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FootstepSurfaceMap", menuName = "Audio/Footstep Surface Map")]
public class FootstepSurfaceMap : ScriptableObject
{
    [Serializable]
    public struct TerrainEntry
    {
        public TerrainLayer layer;
        public FootstepSurface surface;
    }
    
    [Tooltip("Used when nothing else matches.")]
    public FootstepSurface defaultSurface;

    [Tooltip("Terrain paint layer (surface). Only used for Unity Terrain.")]
    public List<TerrainEntry> terrainLayers = new();

    public FootstepSurface Resolve(RaycastHit hit)
    {
        var collider = hit.collider;
        if(collider == null) return null;

        SurfaceMarker marker = collider.GetComponentInParent<SurfaceMarker>();
        if(marker != null && marker.Surface != null) return marker.Surface;

        if (terrainLayers.Count > 0 && collider.TryGetComponent(out Terrain terrain))
        {
            FootstepSurface fromTerrain = ResolveTerrain(terrain, hit.point);
            if(fromTerrain != null) return fromTerrain;
        }
        
        return defaultSurface;
    }

    private FootstepSurface ResolveTerrain(Terrain terrain, Vector3 worldPoint)
    {
        TerrainData data = terrain.terrainData;
        if(data == null || data.alphamapLayers == 0) return null;
        
        // World position -> alphamap (splat map) in pixel.
        Vector3 local = worldPoint - terrain.transform.position;
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(local.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);

        // Result is [height, width, layer]. A 1x1 sample is tiny, and a step only happens ~2-3 times per second.
        float[,,] weights = data.GetAlphamaps(x, z, 1, 1);

        int dominant = 0;
        float best = 0f;

        for (int i = 0; i < weights.GetLength(2); i++)
        {
            if(!(weights[0, 0, 1] > best)) continue;
            best = weights[0, 0, i];
            dominant = i;
        }

        TerrainLayer[] layers = data.terrainLayers;
        if(dominant >= layers.Length) return null;
        
        TerrainLayer dominantLayer = layers[dominant];
        for (int i = 0; i < terrainLayers.Count; i++)
        {
            if(terrainLayers[i].layer == dominantLayer) return terrainLayers[i].surface;
        }
        
        return null;
    }
}