using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterTrail : MonoBehaviour
{
    [SerializeField] private float activeTime = 2f;
    [SerializeField] private Transform character;
    
    [Header("Mesh Settings")]
    [SerializeField] private float meshRefreshRate = 0.1f;
    [SerializeField] private float delay = 3f;

    [Header("Shader")] 
    [SerializeField] private Material trailMat;
    [SerializeField] private string alphaRef = "_Alpha";
    [SerializeField] private float alphaRate = 0.1f;
    [SerializeField] private float alphaRefreshRate = 0.05f;
    
    private bool _isTrailActive;
    private SkinnedMeshRenderer[] _trailMeshRenderers;

    // private void Update()
    // {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
           // ActivateTrail();
        // }
    // }

    // TODO: Call in Animation Event or another player actions
    [Obsolete("Obsolete")]
    public void ActivateTrail()
    {
        if(_isTrailActive)
            return;
        
        _isTrailActive = true;
        StartCoroutine(ActivateTrailCo(activeTime));
    }

    [Obsolete("Obsolete")]
    private IEnumerator ActivateTrailCo(float time)
    {
        while (time > 0)
        {
            time -= meshRefreshRate;
            
            _trailMeshRenderers ??= GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < _trailMeshRenderers.Length; i++)
            {
                var go = new GameObject($"Character Trail {i}");
                go.transform.SetPositionAndRotation(character.position, character.rotation);
                
                var goMr = go.AddComponent<MeshRenderer>();
                var goMf = go.AddComponent<MeshFilter>();
                
                Mesh mesh = new Mesh();
                _trailMeshRenderers[i].BakeMesh(mesh);
                
                goMf.sharedMesh = mesh;
                goMr.sharedMaterial = trailMat;
                goMr.castShadows = false; // disable shadow

                StartCoroutine(AnimateTrailCo(goMr.material, 0, alphaRate, alphaRefreshRate));
                
                Destroy(go, delay);
            }
            
            yield return new WaitForSeconds(meshRefreshRate);
        }
        _isTrailActive = false;
    }

    private IEnumerator AnimateTrailCo(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(alphaRef);

        while (valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(alphaRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
