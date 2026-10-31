using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Имена эффектов. Каждому соответствует префаб Assets/Resources/FX/FX_&lt;имя&gt;.
/// </summary>
public enum VfxId
{
    Spawn,
    Hit,
    HitBurst,
    Death,
    Dash
}

/// <summary>
/// Создаёт визуальные эффекты (системы частиц) из Assets/Resources/FX.
/// Эффекты удаляются сами через несколько секунд.
/// </summary>
public static class Vfx
{
    private const string Folder = "FX/";

    private static readonly Dictionary<VfxId, GameObject> Prefabs = new Dictionary<VfxId, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Prefabs.Clear();
    }

    public static GameObject Spawn(VfxId id, Vector3 position)
    {
        return Spawn(id, position, Quaternion.identity);
    }

    public static GameObject Spawn(VfxId id, Vector3 position, Quaternion rotation, float lifetime = 2.5f)
    {
        // Префаб грузится из Resources один раз и дальше берётся из кэша
        if (!Prefabs.TryGetValue(id, out GameObject prefab) || prefab == null)
        {
            prefab = Resources.Load<GameObject>(Folder + "FX_" + id);
            if (prefab == null)
            {
                Debug.LogWarning($"Vfx: не найден эффект Resources/{Folder}FX_{id}");
                return null;
            }

            Prefabs[id] = prefab;
        }

        GameObject instance = Object.Instantiate(prefab, position, rotation);
        Object.Destroy(instance, lifetime); // отложенное удаление: эффект исчезнет сам
        return instance;
    }
}
