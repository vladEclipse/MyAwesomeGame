using UnityEngine;

/// <summary>
/// Золотая монетка (префаб Coin). Выпадает из убитого моба, крутится и покачивается,
/// сама подлетает к игроку, когда он рядом, и добавляется в счётчик CoinCounter.
/// </summary>
public class Coin : MonoBehaviour
{
    private const float MagnetRadius = 3.5f;
    private const float CollectRadius = 0.85f;
    private const float PickupDelay = 0.35f;
    private const float PopTime = 0.25f;
    private const float SpinSpeed = 220f;

    [Tooltip("Модель монетки: появляется с лёгким «прыжком» размера и покачивается")]
    [SerializeField] private Transform visual;

    private Vector3 baseScale;
    private float age;
    private float phase;
    private bool collected;

    /// <summary>Создаёт монетку из префаба у точки: сажает её на пол под этой точкой.</summary>
    public static Coin Spawn(Coin prefab, Vector3 position)
    {
        return prefab != null ? Instantiate(prefab, FitToGround(position), Quaternion.identity) : null;
    }

    private void Awake()
    {
        phase = Random.value * 6.28f;
        baseScale = visual.localScale;
        visual.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (collected) return;

        float dt = Time.deltaTime;
        age += dt;

        transform.Rotate(0f, SpinSpeed * dt, 0f, Space.World);

        // Появление: монетка «выскакивает» с лёгким перелётом по размеру
        float pop = Mathf.Clamp01(age / PopTime);
        float scale = pop < 1f ? 1f + Mathf.Sin(pop * Mathf.PI) * 0.35f : 1f;
        visual.localScale = baseScale * (pop * scale);
        visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * 3f + phase) * 0.1f);

        if (age < PickupDelay) return;

        PlayerController player = PlayerController.Instance;
        if (player == null || (player.PlayerHealth != null && player.PlayerHealth.IsDead)) return;

        Vector3 target = player.transform.position;
        float distance = Vector3.Distance(transform.position, target);

        if (distance <= CollectRadius)
        {
            Collect();
        }
        else if (distance <= MagnetRadius)
        {
            // Чем ближе игрок, тем быстрее летит монетка
            float speed = Mathf.Lerp(4f, 18f, 1f - distance / MagnetRadius);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * dt);
        }
    }

    private void Collect()
    {
        collected = true;

        Vfx.Spawn(VfxId.HitBurst, transform.position);
        Sfx.Play(SfxId.Coin, 0.45f, 0f);

        if (CoinCounter.Instance != null) CoinCounter.Instance.Add(1);

        Destroy(gameObject);
    }

    // Подставляем точку на пол (мобы и игрок землёй не считаются) и поднимаем на высоту «груди»
    private static Vector3 FitToGround(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 10f, Layers.World, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.75f;
        }
        return position;
    }
}
