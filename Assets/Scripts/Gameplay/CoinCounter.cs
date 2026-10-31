using TMPro;
using UnityEngine;

/// <summary>
/// Счётчик монет и условие победы: набрали GoalCoins монет, и игра выиграна.
/// Компонент висит на GameCanvas, весь интерфейс (счётчик, экран победы) лежит в сцене под ним.
/// </summary>
public class CoinCounter : MonoBehaviour
{
    public const int GoalCoins = 10;

    public static CoinCounter Instance { get; private set; }

    [Header("Интерфейс (объекты под GameCanvas)")]
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text winSubtitle;

    public int Count { get; private set; }
    public bool Won { get; private set; }

    private float punch; // сила «подпрыгивания» счётчика при новой монете, затухает до 0

    // Сброс статики при запуске: при быстром входе в Play Mode она не обнуляется сама
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        ResetState();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (punch > 0f && counterText != null)
        {
            punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * 4f);
            counterText.rectTransform.localScale = Vector3.one * (1f + punch * 0.3f);
        }
    }

    public void Add(int amount)
    {
        if (Won || amount <= 0) return;

        Count += amount;
        punch = 1f;
        UpdateText();

        if (Count >= GoalCoins) Win();
    }

    private void ResetState()
    {
        Count = 0;
        Won = false;
        punch = 0f;
        if (winPanel != null) winPanel.SetActive(false);
        if (counterText != null) counterText.rectTransform.localScale = Vector3.one;
        UpdateText();
    }

    private void UpdateText()
    {
        if (counterText != null) counterText.text = $"Монеты: {Count} / {GoalCoins}";
    }

    private void Win()
    {
        Won = true;

        if (winSubtitle != null) winSubtitle.text = $"Вы собрали {GoalCoins} монет";
        if (winPanel != null) winPanel.SetActive(true);

        // Останавливаем игру и убираем управление (GameUI сам не откроет паузу после победы)
        Time.timeScale = 0f;
        if (PlayerController.Instance != null) PlayerController.Instance.SetControlEnabled(false);

        Sfx.Play(SfxId.Win, 0.6f, 0f);
    }

    /// <summary>Вызывается кнопкой «Играть снова» (OnClick в инспекторе).</summary>
    public void Restart() => GameFlow.Restart();
}
