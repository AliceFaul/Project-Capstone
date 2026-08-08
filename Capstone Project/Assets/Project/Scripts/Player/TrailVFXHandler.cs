using UnityEngine;

public class TrailVFXHandler : MonoBehaviour
{
    [SerializeField] private ParticleSystem trailParticles;

    public void Attach(Transform trailPoint)
    {
        transform.SetParent(trailPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
    
    public void PlayTrail()
    {
        trailParticles.Clear();
        trailParticles.Play();
    }
    
    public void StopTrail()
    {
        trailParticles.Stop();
    }
}