using UnityEngine;
using System.Collections.Generic;

namespace AsakuShop.Customers
{
    public class CustomerQueue : MonoBehaviour
    {
        private Queue<CustomerAgent> queue = new();

        public void AddCustomer(CustomerAgent customer)
        {
            queue.Enqueue(customer);
            Debug.Log($"[QUEUE] Customer added. Queue size: {queue.Count}");
        }

        public CustomerAgent GetNextCustomer()
        {
            if (queue.Count > 0)
            {
                CustomerAgent next = queue.Dequeue();
                Debug.Log($"[QUEUE] Customer called to checkout. Queue size: {queue.Count}");
                return next;
            }
            return null;
        }

        public int GetQueueSize() => queue.Count;
        public bool IsEmpty() => queue.Count == 0;
    }
}