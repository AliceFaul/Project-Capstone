using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    private CinemachineImpulseSource _impulseSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void ShakeCamera()
    {
        _impulseSource.GenerateImpulse();
    }
}