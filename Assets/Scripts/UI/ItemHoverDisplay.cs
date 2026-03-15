using UnityEngine;
using TMPro;
using AsakuShop.Core;
using AsakuShop.Items;

namespace AsakuShop.UI
{
    public class ItemHoverDisplay : MonoBehaviour
    {
#region Fields
        [SerializeField] private TextMeshProUGUI hoverLabel;
        [SerializeField] private Canvas canvas;
#endregion


#region Unity Methods
        void Update()
        {
            if (GameStateController.Instance.CurrentPhase != GamePhase.Playing)
            {
                HideLabel();
                return;
            }
            CheckItemHover();
        }
    
#endregion


#region Private Methods
        private void CheckItemHover()
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f, LayerMask.GetMask("Item"), QueryTriggerInteraction.Ignore))
            {
                ItemPickup itemPickup = hit.collider.GetComponent<ItemPickup>();
                if (itemPickup != null && itemPickup.itemInstance != null)
                {
                    ShowLabel(itemPickup.itemInstance);                    
                    return;
                }
            }

            HideLabel();
        }

        private void ShowLabel(ItemInstance itemInstance)
        {
            if (hoverLabel == null)                
                return;
            
            hoverLabel.text = itemInstance.Definition.DisplayName;
            canvas.gameObject.SetActive(true);
        }

        private void HideLabel()
        {
            hoverLabel.text = "";
        }
#endregion
    }
}
