using System.Collections.Generic;
using UnityEngine;

namespace AsakuShop.Core
{
    // ScriptableObject that holds a list of dialogue lines.
    // Call GetRandomLine() to retrieve a random line for display.
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "AsakuShop/Dialogue")]
    public class Dialogue : ScriptableObject
    {
        [System.Serializable]
        private struct Line
        {
            [TextArea(3, 5)]
            public string Text;
        }

        [SerializeField] private List<Line> lines;

        // Returns a random dialogue line from the list.
        // Returns an empty string and logs a warning if the list is empty.
        public string GetRandomLine()
        {
            if (lines == null || lines.Count == 0)
            {
                Debug.LogWarning($"[Dialogue] '{name}' has no lines configured.");
                return string.Empty;
            }

            return lines[Random.Range(0, lines.Count)].Text;
        }
    }
}
