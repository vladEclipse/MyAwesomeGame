using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] private Health playerHealth;

    [Header("Полоска здоровья")]
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Text healthText;
    [SerializeField] private Color lowHealthColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color fullHealthColor = new Color(0.25f, 0.8f, 0.3f);
    [Tooltip("Скорость плавного изменения полоски (доля шкалы в секунду)")]
    [SerializeField] private float barSpeed = 1.2f;

    [Header("Эффект урона")]
    [SerializeField] private Image damageFlash;
    [SerializeField] private float flashStrength = 0.35f;
    [SerializeField] private float flashFadeSpeed = 0.8f;

    [Header("Панели")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Кнопки")]
    [SerializeField] private Button gameOverRestartButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button quitButton;

    private float shownValue;
    private float targetValue;
    private float flashAlpha;
    private bool isPaused;
    private bool isGameOver;

    private void Start()
    {
        Time.timeScale = 1f;

        if (playerHealth == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerHealth = player.GetComponent<Health>();
        }

        SetActive(gameOverPanel, false);
        SetActive(pausePanel, false);

        Bind(gameOverRestartButton, Restart);
        Bind(pauseRestartButton, Restart);
        Bind(resumeButton, Resume);
        Bind(quitButton, Quit);

        if (playerHealth != null)
        {
            playerHealth.Changed += OnHealthChanged;
            playerHealth.Damaged += OnDamaged;
            playerHealth.Died += OnPlayerDied;

            targetValue = shownValue = playerHealth.Normalized;
            UpdateText(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else
        {
            Debug.LogWarning("GameUI: не найден Health игрока.", this);
        }

        ApplyBar();
        ApplyFlash();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Changed -= OnHealthChanged;
            playerHealth.Damaged -= OnDamaged;
            playerHealth.Died -= OnPlayerDied;
        }

        // На случай выхода из паузы через перезагрузку сцены
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!isGameOver && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }

        // Unscaled-время, чтобы интерфейс жил и во время паузы
        float dt = Time.unscaledDeltaTime;

        if (!Mathf.Approximately(shownValue, targetValue))
        {
            shownValue = Mathf.MoveTowards(shownValue, targetValue, dt * barSpeed);
            ApplyBar();
        }

        if (flashAlpha > 0f)
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * flashFadeSpeed);
            ApplyFlash();
        }
    }

    // Здоровье

    private void OnHealthChanged(float current, float max)
    {
        targetValue = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        UpdateText(current, max);
    }

    private void OnDamaged(float amount)
    {
        flashAlpha = flashStrength;
        ApplyFlash();
    }

    private void OnPlayerDied(Health _)
    {
        isGameOver = true;
        SetActive(gameOverPanel, true);

        // Управление игроком отключаем, остановив его на месте
        PlayerController controller = playerHealth.GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        PlayerAttack attack = playerHealth.GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        Rigidbody body = playerHealth.GetComponent<Rigidbody>();
        if (body != null)
        {
            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(0f, velocity.y, 0f);
        }
    }

    private void ApplyBar()
    {
        if (healthFill != null)
        {
            healthFill.anchorMax = new Vector2(Mathf.Clamp01(shownValue), 1f);
        }

        if (healthFillImage != null)
        {
            healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, shownValue);
        }
    }

    private void ApplyFlash()
    {
        if (damageFlash == null) return;

        Color c = damageFlash.color;
        c.a = flashAlpha;
        damageFlash.color = c;
    }

    private void UpdateText(float current, float max)
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    // Пауза и меню

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetActive(pausePanel, true);
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetActive(pausePanel, false);
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Вспомогательное

    private static void SetActive(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }
}
