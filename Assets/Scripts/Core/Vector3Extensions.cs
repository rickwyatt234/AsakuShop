using UnityEngine;

namespace AsakuShop.Core
{
    // Extension methods for UnityEngine.Vector3.
    public static class Vector3Extensions
    {
        // Returns a copy of this vector with the Y component set to zero.
        // Useful for horizontal look-at calculations on the XZ plane.
        public static Vector3 Flatten(this Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
