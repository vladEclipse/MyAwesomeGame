using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Одноразовая настройка проекта после рефакторинга. После запуска этот файл можно удалить.
/// Меню: Tools > One-Time Setup
/// </summary>
public static class OneTimeSetup
{
    private const string LayerName = "Actors";
    private const int LayerIndex = 7;

    [MenuItem("Tools/One-Time Setup")]
    private static void Run()
    {
        var log = new List<string>();

        // 1. Слой Actors
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        SerializedProperty slot = layers.GetArrayElementAtIndex(LayerIndex);
        if (LayerMask.NameToLayer(LayerName) < 0)
        {
            if (!string.IsNullOrEmpty(slot.stringValue))
            {
                Debug.LogError($"OneTimeSetup: слот {LayerIndex} занят слоем '{slot.stringValue}', ничего не сделано");
                return;
            }
            slot.stringValue = LayerName;
            tagManager.ApplyModifiedProperties();
            log.Add("создан слой Actors");
        }
        int actors = LayerMask.NameToLayer(LayerName);

        // 2. Слой на префабах мобов
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int changed = SetLayer(root, actors);
            if (changed > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            log.Add($"{path}: слой Actors на {changed} объектах");
        }

        // 3. Сцена: игрок, спавнер, лишняя вспышка урона
        var scene = EditorSceneManager.GetActiveScene();

        PlayerController player = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            Undo.RecordObjects(player.GetComponentsInChildren<Transform>(true).Select(t => (Object)t.gameObject).ToArray(), "Actors layer");
            log.Add($"игрок: слой Actors на {SetLayer(player.gameObject, actors)} объектах");
        }
        else log.Add("ВНИМАНИЕ: игрок в сцене не найден");

        foreach (EnemySpawner spawner in Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(spawner);
            var list = new List<Object>();
            Object main = so.FindProperty("enemyPrefab").objectReferenceValue;
            if (main != null) list.Add(main);
            SerializedProperty extra = so.FindProperty("extraEnemyPrefabs");
            for (int i = 0; i < extra.arraySize; i++)
            {
                Object o = extra.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null && !list.Contains(o)) list.Add(o);
            }

            SerializedProperty target = so.FindProperty("enemyPrefabs");
            target.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) target.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedProperties();
            log.Add($"{spawner.name}: в enemyPrefabs перенесено {list.Count} префабов ({string.Join(", ", list.Select(o => o.name))})");
        }

        GameUI ui = Object.FindAnyObjectByType<GameUI>(FindObjectsInactive.Include);
        if (ui != null)
        {
            var so = new SerializedObject(ui);
            Object flash = so.FindProperty("damageFlash")?.objectReferenceValue;
            if (flash is Component c && c.gameObject.name == "DamageFlash")
            {
                Undo.DestroyObjectImmediate(c.gameObject);
                log.Add("удалён объект GameCanvas/DamageFlash (вспышку урона теперь делает только виньетка ScreenFX)");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("OneTimeSetup готово:\n- " + string.Join("\n- ", log));
    }

    // Ставит слой всем объектам на слое Default (UI и прочие особые слои не трогаем)
    private static int SetLayer(GameObject root, int layer)
    {
        int count = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.layer != 0) continue;
            t.gameObject.layer = layer;
            count++;
        }
        return count;
    }
}
