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
            if (Instance == null)
            {
                GameObject go = new GameObject("[PlacementVisualManager]");
                Instance = go.AddComponent<PlacementVisualManager>();
                DontDestroyOnLoad(go);
            }
            Instance = this;
        }
    }
}

