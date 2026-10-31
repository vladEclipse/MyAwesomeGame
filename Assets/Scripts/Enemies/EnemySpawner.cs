using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Создаёт мобов вокруг себя. Когда моб умирает - роняет монетку и через несколько секунд создаёт нового.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Виды мобов. Каждый раз выбирается случайный")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [Tooltip("Монетка, которая выпадает из убитого моба")]
    [SerializeField] private Coin coinPrefab;
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

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: не назначены префабы мобов", this);
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

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (prefab == null) return;

        // Агент ставится на NavMesh с учётом baseOffset, поэтому создаём чуть выше поверхности
        float baseOffset = prefab.TryGetComponent(out NavMeshAgent prefabAgent) ? prefabAgent.baseOffset : 0f;

        GameObject enemy = Instantiate(prefab, point + Vector3.up * baseOffset, Quaternion.identity);

        // Узнаём о смерти моба через событие, чтобы уронить монетку и запустить респавн
        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null) enemyHealth.Died += OnEnemyDied;
    }

    // Случайная точка: в радиусе спавнера, на NavMesh и не слишком близко к игроку
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

        // Из каждого убитого моба выпадает монетка
        Coin.Spawn(coinPrefab, dead.transform.position);

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
