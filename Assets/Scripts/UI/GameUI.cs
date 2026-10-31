using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Интерфейс игрока: полоска здоровья, экран проигрыша и пауза (Esc).
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] private Health playerHealth;

    [Header("Полоска здоровья")]
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Color lowHealthColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color fullHealthColor = new Color(0.25f, 0.8f, 0.3f);
    [Tooltip("Скорость плавного изменения полоски (доля шкалы в секунду)")]
    [SerializeField] private float barSpeed = 1.2f;

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
    private bool isPaused;
    private bool isGameOver;

    private void Start()
    {
        Time.timeScale = 1f;

        if (playerHealth == null && PlayerController.Instance != null)
        {
            playerHealth = PlayerController.Instance.PlayerHealth;
        }

        SetActive(gameOverPanel, false);
        SetActive(pausePanel, false);

        Bind(gameOverRestartButton, GameFlow.Restart);
        Bind(pauseRestartButton, GameFlow.Restart);
        Bind(resumeButton, Resume);
        Bind(quitButton, GameFlow.Quit);

        if (playerHealth != null)
        {
            playerHealth.Changed += OnHealthChanged;
            playerHealth.Died += OnPlayerDied;

            targetValue = shownValue = playerHealth.Normalized;
            UpdateText(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else
        {
            Debug.LogWarning("GameUI: не найден Health игрока.", this);
        }

        ApplyBar();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Changed -= OnHealthChanged;
            playerHealth.Died -= OnPlayerDied;
        }
    }

    private void Update()
    {
        bool won = CoinCounter.Instance != null && CoinCounter.Instance.Won;
        if (!isGameOver && !won && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }

        // Полоска не прыгает, а плавно движется к новому значению.
        // Unscaled-время, чтобы интерфейс жил и во время паузы
        if (!Mathf.Approximately(shownValue, targetValue))
        {
            shownValue = Mathf.MoveTowards(shownValue, targetValue, Time.unscaledDeltaTime * barSpeed);
            ApplyBar();
        }
    }

    // Здоровье

    private void OnHealthChanged(float current, float max)
    {
        targetValue = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        UpdateText(current, max);
    }

    private void OnPlayerDied(Health _)
    {
        isGameOver = true;
        SetActive(gameOverPanel, true);

        if (playerHealth.TryGetComponent(out PlayerController controller)) controller.SetControlEnabled(false);
    }

    private void ApplyBar()
    {
        // Ширина заливки задаётся правым якорем: 0 - пусто, 1 - полная ширина
        if (healthFill != null)
        {
            healthFill.anchorMax = new Vector2(Mathf.Clamp01(shownValue), 1f);
        }

        if (healthFillImage != null)
        {
            healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, shownValue);
        }
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
        Time.timeScale = 0f; // останавливает физику, Time.deltaTime и всё, что от них зависит
        SetActive(pausePanel, true);
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetActive(pausePanel, false);
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
