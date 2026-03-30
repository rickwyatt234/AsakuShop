using UnityEngine;
using Unity.AI.Navigation;

public class NavMeshBuilder : MonoBehaviour
{
    [SerializeField] private NavMeshSurface surface;

    private void Start()
    {
        if (surface == null)
            surface = GetComponent<NavMeshSurface>();

        if (surface == null)
        {
            Debug.LogError("[NavMeshBuilder] No NavMeshSurface assigned or found!");
            return;
        }

        surface.BuildNavMesh();
        Debug.Log("[NavMeshBuilder] Initial NavMesh baked.");
    }
}