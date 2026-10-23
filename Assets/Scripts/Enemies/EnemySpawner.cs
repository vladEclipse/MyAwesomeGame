using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
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
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null) player = playerController.transform;

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
        float baseOffset = 0f;
        NavMeshAgent prefabAgent = enemyPrefab.GetComponent<NavMeshAgent>();
        if (prefabAgent != null) baseOffset = prefabAgent.baseOffset;

        GameObject enemy = Instantiate(enemyPrefab, point + Vector3.up * baseOffset, Quaternion.identity);

        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null) enemyHealth.Died += OnEnemyDied;
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
