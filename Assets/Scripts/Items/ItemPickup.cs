using UnityEngine;
using AsakuShop.Core;
using AsakuShop.Player;
using AsakuShop.UI;

namespace AsakuShop.Items
{
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        public ItemInstance ItemInstance { get; private set; }
        private PlayerHands playerHands;

        public void Initialize(ItemInstance itemInstance)
        {
            ItemInstance = itemInstance;
        }

        private void Start()
        {
            playerHands = FindFirstObjectByType<PlayerHands>();
            if (playerHands == null)
            {
                Debug.LogError("PlayerHands component not found in the scene. Make sure there is a Player with PlayerHands.");
            }
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

