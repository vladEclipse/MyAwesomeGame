using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простая атака игрока: удар по области вокруг персонажа.
/// ЛКМ или F. Направление не нужно, поэтому работает при любом ракурсе камеры.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float cooldown = 0.45f;
    [SerializeField] private LayerMask hitMask = ~0;

    private readonly HashSet<Health> hitTargets = new HashSet<Health>();
    private readonly Collider[] buffer = new Collider[32];

    private Health ownHealth;
    private float nextAttackTime;

    private void Awake()
    {
        ownHealth = GetComponent<Health>();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (ownHealth != null && ownHealth.IsDead) return;
        if (Time.time < nextAttackTime) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F))
        {
            Attack();
        }
    }

    private void Attack()
    {
        nextAttackTime = Time.time + cooldown;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            buffer,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        hitTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            Health target = buffer[i].GetComponentInParent<Health>();
            if (target == null || target == ownHealth || target.IsDead) continue;

            // У моба может быть несколько коллайдеров — бьём каждого только один раз.
            if (hitTargets.Add(target))
            {
                target.TakeDamage(damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
