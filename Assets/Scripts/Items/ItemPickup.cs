using UnityEngine;
using AsakuShop.Core;
using AsakuShop.UI;

namespace AsakuShop.Items
{
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        public ItemInstance ItemInstance { get; private set; }
        private IPickupTarget pickupTarget;;

        public void Initialize(ItemInstance itemInstance)
        {
            ItemInstance = itemInstance;
        }

        private void Start()
        {
            pickupTarget = PlayerService.PickupTarget;
            if (pickupTarget == null)
                Debug.LogError("[ItemPickup] No IPickupTarget registered in PlayerService. Make sure PlayerHands is in the scene.");
        }

#region IInteractable implementation
        public void OnInteract()
        {
            if (ItemInstance != null)
            {
                playerHands.heldItem = ItemInstance;
                playerHands.heldItemPickup = this;
                playerHands.TryPickupInteractable(gameObject);
            }
        }
        public void OnExamine()
        {
            //If player is holding this item, show the examination UI
             if (playerHands != null && playerHands.heldItem == ItemInstance)
             {
                ItemExaminer.Instance.StartExamination(ItemInstance);
             }
             else if (ItemInstance.IsOnAShelf)
            {
                //Set price from the shelf
            }
        }
#endregion
    }
}

