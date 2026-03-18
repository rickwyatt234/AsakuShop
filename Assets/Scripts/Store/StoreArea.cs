using UnityEngine;

namespace AsakuShop.Store
{
    public class StoreArea : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            IStoreCustomer customer = other.GetComponent<IStoreCustomer>();
            if (customer != null)
            {
                customer.SetInStoreArea(true);
                Debug.Log("[STORE AREA] Customer entered store");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            IStoreCustomer customer = other.GetComponent<IStoreCustomer>();
            if (customer != null)
            {
                customer.SetInStoreArea(false);
                Debug.Log("[STORE AREA] Customer left store");
            }
        }
    }
}