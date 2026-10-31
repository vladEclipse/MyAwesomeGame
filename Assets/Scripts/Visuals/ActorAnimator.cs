using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Передаёт состояние игрока или моба в Animator его модели.
/// Параметры контроллера: Speed (float), Attack и Hit (trigger), Dashing и Dead (bool).
/// Работает и на игроке (Rigidbody + PlayerController), и на мобе (NavMeshAgent + EnemyAI).
/// </summary>
[DisallowMultipleComponent]
public class ActorAnimator : MonoBehaviour
{
    // Имена параметров заранее превращаем в числа: так Animator работает быстрее, чем со строками
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int AttackId = Animator.StringToHash("Attack");
    private static readonly int HitId = Animator.StringToHash("Hit");
    private static readonly int DashingId = Animator.StringToHash("Dashing");
    private static readonly int DeadId = Animator.StringToHash("Dead");

    [Tooltip("Сглаживание параметра Speed, чтобы переходы Idle - Walk - Run были плавными")]
    [SerializeField] private float speedDamping = 0.08f;

    private Animator animator;
    private Health health;
    private NavMeshAgent agent;
    private Rigidbody body;
    private PlayerController player;
    private IAttacker attacker;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        body = GetComponent<Rigidbody>();
        player = GetComponent<PlayerController>();
        attacker = GetComponent<IAttacker>();

        if (animator == null)
        {
            Debug.LogWarning($"{name}: ActorAnimator не нашёл Animator в дочерних объектах (модель не подключена)", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        if (attacker != null) attacker.Attacked += OnAttack;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        if (attacker != null) attacker.Attacked -= OnAttack;
    }

    private void Update()
    {
        float speed = 0f;

        // Скорость берём у агента (моб) или у Rigidbody (игрок)
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            speed = agent.velocity.magnitude;
        }
        else if (body != null && !body.isKinematic)
        {
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            speed = velocity.magnitude;
        }

        animator.SetFloat(SpeedId, speed, speedDamping, Time.deltaTime);

        if (player != null) animator.SetBool(DashingId, player.IsDashing);
    }

    private void OnAttack()
    {
        animator.SetTrigger(AttackId);
    }

    private void OnDamaged(float amount)
    {
        // Смертельный удар обрабатывает OnDied, анимацию получения урона не проигрываем
        if (health != null && health.CurrentHealth <= 0f) return;
        animator.SetTrigger(HitId);
    }

    private void OnDied(Health _)
    {
        animator.SetBool(DeadId, true);
    }
}
