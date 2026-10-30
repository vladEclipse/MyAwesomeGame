using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("Дополнительные виды мобов. Каждый раз выбирается случайный из них и основного префаба")]
    [SerializeField] private GameObject[] extraEnemyPrefabs;
    [SerializeField] private int count = 4;
    [SerializeField] private float spawnRadius = 5.5f;
    [Tooltip("Мобы не появляются ближе этого расстояния от игрока")]
    [SerializeField] private float minDistanceFromPlayer = 4f;

    [Header("Респавн")]
    [SerializeField] private bool respawn = true;
    [SerializeField] private float respawnDelay = 6f;

    private Transform player;

    private void Start()
    {
        if (PlayerController.Instance != null) player = PlayerController.Instance.transform;

        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: не назначен префаб моба", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        if (!TryGetSpawnPoint(out Vector3 point))
        {
            Debug.LogWarning("EnemySpawner: не удалось найти точку на NavMesh. Проверьте, что NavMesh построен", this);
            return;
        }

        // Агент ставится на NavMesh с учётом baseOffset, поэтому создаём чуть выше поверхности
        GameObject prefab = PickPrefab();
        float baseOffset = 0f;
        NavMeshAgent prefabAgent = prefab.GetComponent<NavMeshAgent>();
        if (prefabAgent != null) baseOffset = prefabAgent.baseOffset;

        GameObject enemy = Instantiate(prefab, point + Vector3.up * baseOffset, Quaternion.identity);

        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null) enemyHealth.Died += OnEnemyDied;
    }

    private GameObject PickPrefab()
    {
        int extra = extraEnemyPrefabs != null ? extraEnemyPrefabs.Length : 0;
        if (extra == 0) return enemyPrefab;

        int index = Random.Range(0, extra + 1);
        if (index == 0) return enemyPrefab;

        GameObject chosen = extraEnemyPrefabs[index - 1];
        return chosen != null ? chosen : enemyPrefab;
    }

    private bool TryGetSpawnPoint(out Vector3 result)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;

            if (player != null)
            {
                Vector3 flat = hit.position - player.position;
                flat.y = 0f;
                if (flat.magnitude < minDistanceFromPlayer) continue;
            }

            result = hit.position;
            return true;
        }

        result = default;
        return false;
    }

    private void OnEnemyDied(Health dead)
    {
        dead.Died -= OnEnemyDied;
        if (respawn) StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnOne();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
