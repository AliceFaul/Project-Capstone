using UnityEngine;

/// Put on a collider (or any parent of it) to force the footstep surface there: wooden bridge, stone floor, ...
public class SurfaceMarker : MonoBehaviour
{
    [SerializeField] private FootstepSurface surface;
    public FootstepSurface Surface => surface;
}