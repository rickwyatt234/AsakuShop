using UnityEngine;
using TMPro;
using AsakuShop.Items;
using AsakuShop.Core;

namespace AsakuShop.UI
{
    public class ItemHoverDisplay : MonoBehaviour
    {
        public static ItemHoverDisplay Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI hoverLabel;
        [SerializeField] private CanvasGroup hoverLabelCanvasGroup;
        [SerializeField] private RectTransform labelRect;
        private ItemInstance currentHoveredItem = null;

        private bool isShowingStorageLabel = false;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (hoverLabel == null)
                hoverLabel = GetComponentInChildren<TextMeshProUGUI>();
            
            if (hoverLabelCanvasGroup == null)
                hoverLabelCanvasGroup = GetComponent<CanvasGroup>();

            if (labelRect == null)
                labelRect = hoverLabel?.GetComponent<RectTransform>();

            ResetDisplay();
        }

        private void Update()
        {
            // Update label position if showing storage label
            if (isShowingStorageLabel && labelRect != null)
            {
                UpdateLabelPosition();
            }
            // Only do world item raycast when inventory is CLOSED
            if (!InventoryState.IsOpen)
            {
                CheckItemHover();
            }
            // If inventory is open but we're NOT showing a storage label, hide it
            else if (!isShowingStorageLabel)
            {
                ResetDisplay();
            }
        }

        private void UpdateLabelPosition()
        {
            // Get mouse position in screen space
            Vector3 mousePos = UnityEngine.Input.mousePosition;
            
            // Offset label to the side of cursor so it doesn't block it
            mousePos.x += 200f;  // Offset right
            mousePos.y += 10f;   // Offset up
            
            // Set position
            labelRect.position = mousePos;
        }

        private void CheckItemHover()
        {
            if (Camera.main == null)
            {
                ResetDisplay();
                return;
            }

            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, 5f, LayerMask.GetMask("Item"));

            ItemInstance hoveredItem = null;

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    ItemInstance itemPickup = hit.collider.GetComponent<ItemInstance>();
                    if (itemPickup != null)
                    {
                        hoveredItem = itemPickup;
                        break;
                    }
                }
            }

            if (hoveredItem != currentHoveredItem)
            {
                currentHoveredItem = hoveredItem;

                if (hoveredItem != null)
                {
                    ShowLabel(hoveredItem);
                }
                else
                {
                    ResetDisplay();
                }
            }
        }

        public void ShowLabel(ItemInstance itemInstance, bool showDetails = false)
        {
            if (itemInstance == null)
            {
                ResetDisplay();
                return;
            }

            if (hoverLabel == null || hoverLabelCanvasGroup == null)
                return;

            // Set text
            if (showDetails)
            {
                isShowingStorageLabel = true;  // Mark that we're showing a storage label
                hoverLabel.text = $"{itemInstance.Definition.DisplayName}\n"
                    + $"Grade: {itemInstance.CurrentGrade.ToDisplayString()} | "
                    + $"¥{itemInstance.CurrentPrice} | "
                    + $"{itemInstance.Definition.WeightKg}kg";
            }
            else
            {
                isShowingStorageLabel = false;
                hoverLabel.text = itemInstance.Definition.DisplayName;
            }

            // Make label visible
            if (hoverLabelCanvasGroup.alpha < 1f)
            {
                hoverLabelCanvasGroup.alpha = 1f;
            }
        }

        public void ShowLabelForContainer(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                ResetDisplay();
                return;
            }

            if (hoverLabel == null || hoverLabelCanvasGroup == null)
                return;

            hoverLabel.text = containerName;
            isShowingStorageLabel = true;

            if (hoverLabelCanvasGroup.alpha < 1f)
            {
                hoverLabelCanvasGroup.alpha = 1f;
            }
        }

        public void HideLabel()
        {
            ResetDisplay();
        }

        public void ResetDisplay()
        {
            currentHoveredItem = null;
            isShowingStorageLabel = false;  // Clear flag

            if (hoverLabel == null || hoverLabelCanvasGroup == null)
                return;

            hoverLabel.text = "";
            if (hoverLabelCanvasGroup.alpha > 0f)
            {
                hoverLabelCanvasGroup.alpha = 0f;
            }
        }
    }
}