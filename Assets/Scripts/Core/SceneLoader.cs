using UnityEngine;

namespace AsakuShop.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static void LoadScene(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            GameBootstrapper.State.RequestTransition(GamePhase.Playing);
        }
    }
}
