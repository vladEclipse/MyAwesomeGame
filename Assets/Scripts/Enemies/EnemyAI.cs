using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour, IAttacker
{
    public enum State
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }

    [Header("Обнаружение")]
    [SerializeField] private float detectionRange = 8f;
    [Tooltip("Дальше этого расстояния моб теряет игрока и возвращается к патрулю")]
    [SerializeField] private float loseRange = 12f;
    [Tooltip("Высота глаз относительно центра моба для проверки прямой видимости")]
    [SerializeField] private float eyeOffset = 0.5f;
    [SerializeField] private LayerMask sightMask = ~0;

    [Header("Патруль")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolSpeed = 1.8f;
    [SerializeField] private Vector2 idleTimeRange = new Vector2(1f, 3f);

    [Header("Преследование")]
    [SerializeField] private float chaseSpeed = 3.6f;

    [Header("Атака")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.2f;
    [Tooltip("Пауза перед первым ударом после сближения")]
    [SerializeField] private float firstAttackDelay = 0.4f;

    [Header("Смерть")]
    [SerializeField] private float destroyDelay = 1.5f;

    private NavMeshAgent agent;
    private Health health;
    private Transform target;
    private Health targetHealth;

    private Vector3 homePosition;
    private State state;
    private bool idling;
    private float idleUntil;
    private float nextAttackTime;

    public State CurrentState => state;

    /// Вызывается в момент удара по игроку (для анимации)
    public event System.Action Attacked;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void Start()
    {
        homePosition = transform.position;

        PlayerController player = PlayerController.Instance;
        if (player != null)
        {
            target = player.transform;
            targetHealth = player.PlayerHealth;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}: моб не стоит на NavMesh. Проверьте, что NavMesh построен и моб создан на нём", this);
            enabled = false;
            return;
        }

        EnterPatrol();
    }

    private void Update()
    {
        if (state == State.Dead) return;

        // Нет цели или игрок погиб - просто патрулируем
        if (target == null || targetHealth == null || targetHealth.IsDead)
        {
            if (state != State.Patrol) EnterPatrol();
            TickPatrol();
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        switch (state)
        {
            case State.Patrol:
                if (distance <= detectionRange && HasLineOfSight())
                {
                    EnterChase();
                }
                else
                {
                    TickPatrol();
                }
                break;

            case State.Chase:
                if (distance > loseRange)
                {
                    EnterPatrol();
                }
                else if (distance <= attackRange)
                {
                    EnterAttack();
                }
                else
                {
                    agent.SetDestination(target.position);
                }
                break;

            case State.Attack:
                // Небольшой запас, чтобы моб не дёргался на границе дистанции атаки
                if (distance > attackRange * 1.25f)
                {
                    EnterChase();
                }
                else
                {
                    FaceTarget(toTarget);
                    TryAttack();
                }
                break;
        }
    }

    // Переходы между состояниями

    private void EnterPatrol()
    {
        state = State.Patrol;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.1f;
        PickPatrolPoint();
    }

    private void EnterChase()
    {
        if (!agent.isOnNavMesh) return;

        state = State.Chase;
        idling = false;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
    }

    private void EnterAttack()
    {
        state = State.Attack;
        agent.isStopped = true;
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + firstAttackDelay);
    }

    // Патруль

    private void TickPatrol()
    {
        if (idling)
        {
            if (Time.time >= idleUntil) PickPatrolPoint();
            return;
        }

        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.15f)
        {
            idling = true;
            idleUntil = Time.time + Random.Range(idleTimeRange.x, idleTimeRange.y);
        }
    }

    private void PickPatrolPoint()
    {
        idling = false;

        for (int i = 0; i < 10; i++)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = homePosition + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        // Подходящую точку не нашли - постоим и попробуем позже
        idling = true;
        idleUntil = Time.time + 1f;
    }

    // Атака

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        Attacked?.Invoke();
        targetHealth.TakeDamage(attackDamage);
    }

    private void FaceTarget(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, agent.angularSpeed * Time.deltaTime);
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * eyeOffset;
        Vector3 direction = target.position - origin;
        float distance = direction.magnitude;
        if (distance < 0.01f) return true;

        // Видимость блокирует только мир: игрок и мобы на слое Actors исключены
        return !Physics.Raycast(origin, direction / distance, distance, sightMask & Layers.World, QueryTriggerInteraction.Ignore);
    }

    // События здоровья

    private void OnDamaged(float amount)
    {
        // Получил урон во время патруля - сразу начинает охоту, даже если игрок далеко
        if (state == State.Patrol && target != null && targetHealth != null && !targetHealth.IsDead)
        {
            EnterChase();
        }
    }

    private void OnDied(Health _)
    {
        state = State.Dead;

        if (agent.enabled) agent.enabled = false;

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // Сначала доигрывается анимация смерти, и только в последние полсекунды моб уменьшается
        const float shrinkTime = 0.5f;
        float lingerTime = Mathf.Max(0f, destroyDelay - shrinkTime);
        if (lingerTime > 0f) yield return new WaitForSeconds(lingerTime);

        Vector3 startScale = transform.localScale;
        float duration = Mathf.Min(shrinkTime, destroyDelay);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}