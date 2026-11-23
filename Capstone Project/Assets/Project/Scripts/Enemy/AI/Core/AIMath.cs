using UnityEngine;

namespace Mhieu.Enemy
{
    internal static class AIMath
    {
        /// <summary>Khoảng cách trên mặt phẳng XZ.</summary>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
