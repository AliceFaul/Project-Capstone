using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private CinemachineCamera virtualCamera;

    private float _targetSize;
    private float _zoomVelocity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _targetSize = virtualCamera.Lens.OrthographicSize;
    }

    private void Update()
    {
        if (virtualCamera == null) return;

        var lens = virtualCamera.Lens;

        lens.OrthographicSize = Mathf.SmoothDamp(
            lens.OrthographicSize,
            _targetSize,
            ref _zoomVelocity,
            0.25f 
        );

        virtualCamera.Lens = lens;
    }

    public void SetTarget(Transform newTarget)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = newTarget;
            virtualCamera.LookAt = newTarget;
        }
        else
        {
            Debug.LogWarning("[CameraController] Virtual Camera is not assigned.");
        }
    }

    public void SetOrthographicSize(float newSize)
    {
        _targetSize = newSize;
    }
}