using UnityEngine;

namespace AsakuShop.UI
{
    public class PlacementVisualManager : MonoBehaviour
    {
        public static PlacementVisualManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}

