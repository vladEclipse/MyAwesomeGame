using UnityEngine;

public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private RectTransform fill;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private bool hideWhenFull = true;

    private Camera cam;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.Changed += OnChanged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Changed -= OnChanged;
        health.Died -= OnDied;
    }

    private void Start()
    {
        cam = Camera.main;
        Refresh();
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }

    private void OnChanged(float current, float max)
    {
        Refresh();
    }

    private void OnDied(Health _)
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        if (health == null) return;

        float value = Mathf.Clamp01(health.Normalized);

        if (fill != null) fill.anchorMax = new Vector2(value, 1f);
        if (group != null) group.alpha = hideWhenFull && value >= 0.999f ? 0f : 1f;
    }
}
