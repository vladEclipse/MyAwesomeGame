using Unity.AI.Navigation;
using UnityEngine;


[RequireComponent(typeof(NavMeshSurface))]
public class RuntimeNavMeshBuilder : MonoBehaviour
{
    private void Awake()
    {
        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface.navMeshData == null)
        {
            surface.BuildNavMesh();
        }
    }
}
