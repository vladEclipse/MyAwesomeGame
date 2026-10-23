using System.Collections;
using UnityEngine;

/// <summary>
/// Короткая вспышка цвета при получении урона
/// Использует MaterialPropertyBlock
/// </summary>
[RequireComponent(typeof(Health))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float duration = 0.12f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Health health;
    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private Coroutine routine;

    private void Awake()
    {
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        routine = null;
        Clear();
    }

    private void OnDamaged(float amount)
    {
        if (!isActiveAndEnabled) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        Apply(flashColor);
        yield return new WaitForSeconds(duration);
        Clear();
        routine = null;
    }

    private void Apply(Color color)
    {
        block.SetColor(BaseColorId, color);
        foreach (Renderer r in renderers)
        {
            if (r != null) r.SetPropertyBlock(block);
        }
    }

    private void Clear()
    {
        if (renderers == null) return;
        foreach (Renderer r in renderers)
        {
            if (r != null) r.SetPropertyBlock(null);
        }
    }
}
