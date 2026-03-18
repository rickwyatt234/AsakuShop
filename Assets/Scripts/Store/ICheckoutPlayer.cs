using UnityEngine;

namespace AsakuShop.Store
{
    public interface ICheckoutPlayer
    {
        void SnapToCheckoutPosition(Transform position);
        void LockMovement(bool locked);
    }
}