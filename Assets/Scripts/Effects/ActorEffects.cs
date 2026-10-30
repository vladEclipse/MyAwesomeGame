using UnityEngine;

/// <summary>
/// Звуки, частицы и тряска камеры для игрока и мобов.
/// Подписывается на события Health, PlayerAttack, EnemyAI и PlayerController, поэтому
/// остальные скрипты о нём ничего не знают.
/// </summary>
[DisallowMultipleComponent]
public class ActorEffects : MonoBehaviour
{
    [Header("Шаги (только игрок)")]
    [SerializeField] private float minStepInterval = 0.32f;
    [SerializeField] private float maxStepInterval = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float stepVolume = 0.12f;

    [Header("Тряска камеры")]
    [SerializeField] private float hurtShake = 0.35f;
    [SerializeField] private float enemyDeathShake = 0.15f;

    private Health health;
    private PlayerController player;
    private PlayerAttack playerAttack;
    private EnemyAI enemy;
    private Rigidbody body;

    // Центр и низ персонажа запоминаем заранее: у убитого моба коллайдеры выключаются
    private Vector3 localCenter;
    private Vector3 localFeet;
    private float nextStepTime;

    private Vector3 Center
    {
        get
        {
            Vector3 c = transform.TransformPoint(localCenter);
            c.y = Mathf.Max(c.y, transform.position.y + 0.9f);
            return c;
        }
    }

    private Vector3 Feet
    {
        get
        {
            Vector3 f = transform.TransformPoint(localFeet);
            f.y = Mathf.Max(f.y, transform.position.y + 0.05f);
            return f;
        }
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        player = GetComponent<PlayerController>();
        playerAttack = GetComponent<PlayerAttack>();
        enemy = GetComponent<EnemyAI>();
        body = GetComponent<Rigidbody>();

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            localCenter = transform.InverseTransformPoint(b.center);
            localFeet = transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y + 0.05f, b.center.z));
        }
        else
        {
            localCenter = Vector3.up;
            localFeet = Vector3.zero;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        if (playerAttack != null) playerAttack.Attacked += OnPlayerAttack;
        if (enemy != null) enemy.Attacked += OnEnemyAttack;
        if (player != null) player.Dashed += OnDashed;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        if (playerAttack != null) playerAttack.Attacked -= OnPlayerAttack;
        if (enemy != null) enemy.Attacked -= OnEnemyAttack;
        if (player != null) player.Dashed -= OnDashed;
    }

    private void Start()
    {
        if (enemy != null)
        {
            // Эффект появления моба
            Vfx.Spawn(VfxId.Spawn, Feet);
            Sfx.PlayAt(SfxId.Spawn, transform.position, 0.6f);
        }
    }

    private void Update()
    {
        if (player == null || body == null || player.IsDashing) return;
        if (Time.timeScale <= 0f || (health != null && health.IsDead)) return;

        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (speed > 2f && Time.time >= nextStepTime)
        {
            Sfx.Play(SfxId.Step, stepVolume, 0.2f);
            nextStepTime = Time.time + Mathf.Lerp(maxStepInterval, minStepInterval, Mathf.InverseLerp(2f, 7f, speed));
        }
    }

    // События
    private void OnDamaged(float amount)
    {
        Vfx.Spawn(VfxId.Hit, Center);

        if (player != null)
        {
            Sfx.Play(SfxId.PlayerHurt, 0.9f);
            CameraFollow.Shake(hurtShake, 0.25f);
        }
        else
        {
            Sfx.PlayAt(SfxId.EnemyHit, transform.position, 0.55f);

            // Удар игрока по врагу: яркий разноцветный взрыв частиц по направлению удара
            PlayerController p = PlayerController.Instance;
            Vector3 hitDirection = p != null ? Center - p.transform.position : transform.forward;
            hitDirection.y = 0f;
            Quaternion burstRotation = hitDirection.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hitDirection) : Quaternion.identity;
            Vfx.Spawn(VfxId.HitBurst, Center, burstRotation);
        }
    }

    private void OnDied(Health _)
    {
        Vfx.Spawn(VfxId.Death, Center);

        if (player != null)
        {
            Sfx.Play(SfxId.PlayerDeath);
            return;
        }

        Sfx.PlayAt(SfxId.EnemyDeath, transform.position);

        PlayerController p = PlayerController.Instance;
        if (p != null && Vector3.Distance(p.transform.position, transform.position) < 12f)
        {
            CameraFollow.Shake(enemyDeathShake, 0.15f);
        }
    }

    private void OnPlayerAttack()
    {
        Sfx.Play(SfxId.Swing, 0.6f);
    }

    private void OnEnemyAttack()
    {
        Sfx.PlayAt(SfxId.EnemyAttack, transform.position, 0.9f);
    }

    private void OnDashed(Vector3 direction)
    {
        Sfx.Play(SfxId.Dash, 0.8f);

        // Пыль вылетает назад, против направления рывка
        Quaternion rotation = direction.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(-direction, Vector3.up)
            : Quaternion.identity;
        Vfx.Spawn(VfxId.Dash, Feet, rotation);
    }
}
