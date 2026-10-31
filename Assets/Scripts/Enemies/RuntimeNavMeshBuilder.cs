using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Строит NavMesh (карту проходимых поверхностей для мобов) при старте, если он не запечён заранее.
/// </summary>
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
