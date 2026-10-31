using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Красная виньетка по краям экрана: вспыхивает при уроне и держится, пока у игрока мало здоровья.
/// Висит в сцене на объекте ScreenFX вместе с глобальным Volume (приоритет выше основного),
/// поэтому настройки пост-обработки сцены не трогает.
/// </summary>
[RequireComponent(typeof(Volume))]
public class ScreenFX : MonoBehaviour
{
    public static ScreenFX Instance { get; private set; }

    [Tooltip("Обычная сила виньетки, должна совпадать с Volume в сцене")]
    [SerializeField] private float baseIntensity = 0.2f;
    [SerializeField] private float maxIntensity = 0.55f;
    [SerializeField] private Color damageColor = new Color(0.8f, 0.03f, 0.03f);
    [SerializeField] private float pulseFadeSpeed = 2.2f;

    private Vignette vignette;
    private Health playerHealth;
    private float pulse;
    private float lowHealth;

    private void Awake()
    {
        Instance = this;

        // profile (а не sharedProfile) - своя копия, файл профиля в проекте не меняется
        GetComponent<Volume>().profile.TryGet(out vignette);
    }

    private void Start()
    {
        // Сам следим за здоровьем игрока, остальным скриптам не нужно звать ScreenFX
        if (PlayerController.Instance != null) playerHealth = PlayerController.Instance.PlayerHealth;
        if (playerHealth == null) return;

        playerHealth.Damaged += OnPlayerDamaged;
        playerHealth.Died += OnPlayerDied;
        playerHealth.Changed += OnPlayerHealthChanged;
        SetHealth(playerHealth.Normalized);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (playerHealth != null)
        {
            playerHealth.Damaged -= OnPlayerDamaged;
            playerHealth.Died -= OnPlayerDied;
            playerHealth.Changed -= OnPlayerHealthChanged;
        }
    }

    private void OnPlayerDamaged(float amount) => Pulse(1f);
    private void OnPlayerDied(Health _) => Pulse(1f);
    private void OnPlayerHealthChanged(float current, float max) => SetHealth(max > 0f ? current / max : 0f);

    private void Update()
    {
        if (vignette == null) return;

        if (pulse > 0f) pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * pulseFadeSpeed);

        // Итоговая опасность 0..1: большее из вспышки и малого здоровья. Она задаёт и силу, и цвет виньетки
        float danger = Mathf.Max(pulse, lowHealth);
        vignette.intensity.value = Mathf.Lerp(baseIntensity, maxIntensity, danger);
        vignette.color.value = Color.Lerp(Color.black, damageColor, danger);
    }

    /// <summary>Кратковременная вспышка, сила от 0 до 1.</summary>
    private void Pulse(float strength)
    {
        pulse = Mathf.Max(pulse, Mathf.Clamp01(strength));
    }

    /// <summary>Постоянная красная кайма при малом здоровье. Аргумент - доля здоровья от 0 до 1.</summary>
    private void SetHealth(float normalizedHealth)
    {
        // Начинается с 35% здоровья и достигает 0.7 силы при нуле
        lowHealth = Mathf.InverseLerp(0.35f, 0f, normalizedHealth) * 0.7f;
    }
}
