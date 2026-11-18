using UnityEngine;
using UnityEngine.AI;

public static class InteractionUtil
{
    // True when 'from' is within the target's interact zone (flat distance).
    public static bool IsInRange(Vector3 from, IInteractable target, float tolerance = 1f)
    {
        Vector3 delta = target.InteractPosition - from;
        delta.y = 0f;
        return delta.magnitude <= target.InteractRange * tolerance;
    }

    // A NavMesh point at ~80% of the interact range, on the side the player is coming from
    public static Vector3 GetApproachPoint(Vector3 from, IInteractable target)
    {
        Vector3 center = target.InteractPosition;
        Vector3 direction = from - center;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.back;

        Vector3 wanted = center + direction * (target.InteractRange * 0.8f);
        return NavMesh.SamplePosition(wanted, out var hit, 3f, NavMesh.AllAreas) ? hit.position : wanted;
    }

    public static bool IsAlive(IInteractable target)
    {
        if(target == null) return false;
        return target is not Object unityObject || unityObject != null;
    }
}