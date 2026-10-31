using System.Collections;
using UnityEngine;

/// <summary>
/// Короткая вспышка цвета модели при получении урона.
/// Использует MaterialPropertyBlock: цвет меняется только у этого объекта, копия материала не создаётся.
/// </summary>
[RequireComponent(typeof(Health))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float duration = 0.12f;

    // _BaseColor - основной цвет шейдера URP Lit. Обращаться по числовому ID быстрее, чем по строке
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
        Clear(); // если выключили посреди вспышки, модель не должна остаться белой
    }

    private void OnDamaged(float amount)
    {
        if (!isActiveAndEnabled) return;

        // Повторный удар перезапускает вспышку
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

    // null снимает блок целиком - объект возвращается к обычному материалу
    private void Clear()
    {
        if (renderers == null) return;
        foreach (Renderer r in renderers)
        {
            if (r != null) r.SetPropertyBlock(null);
        }
    }
}
