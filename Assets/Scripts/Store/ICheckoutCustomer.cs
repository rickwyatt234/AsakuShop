using System.Collections.Generic;
using AsakuShop.Items;

namespace AsakuShop.Store
{
    // Interface implemented by Customer to allow CheckoutCounter and StoreManager
    // to interact with customers without creating a circular assembly dependency
    // between AsakuShop.Store and AsakuShop.Customers.
    public interface ICheckoutCustomer
    {
        //The items this customer intends to purchase.
        List<ItemInstance> Inventory { get; }

        //Called by the counter after payment is complete.
        void OnCheckoutComplete();
    }
}
