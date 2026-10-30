using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.12f;

    [Tooltip("Если включено, смещение камеры относительно цели берётся из текущего положения в сцене при старте")]
    [SerializeField] private bool useInitialOffset = true;
    [SerializeField] private Vector3 offset = new Vector3(15f, 10f, 15f);

    private Vector3 velocity;
    private Vector3 basePosition;

    private float shakeTime;
    private float shakeDuration;
    private float shakeMagnitude;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (target == null && PlayerController.Instance != null)
        {
            target = PlayerController.Instance.transform;
        }

        if (target != null && useInitialOffset)
        {
            offset = transform.position - target.position;
        }

        basePosition = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Плавное следование считаем отдельно от тряски, иначе тряска попадала бы в сглаживание
        Vector3 desired = target.position + offset;
        basePosition = Vector3.SmoothDamp(basePosition, desired, ref velocity, smoothTime);

        Vector3 shake = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float strength = Mathf.Clamp01(shakeTime / shakeDuration);
            shake = Random.insideUnitSphere * (shakeMagnitude * strength * strength);
        }

        transform.position = basePosition + shake;
    }

    /// <summary>
    /// Трясёт камеру. Более сильная тряска не перебивается слабой.
    /// </summary>
    public static void Shake(float magnitude, float duration)
    {
        if (Instance == null || duration <= 0f) return;
        Instance.AddShake(magnitude, duration);
    }

    private void AddShake(float magnitude, float duration)
    {
        if (shakeTime > 0f && magnitude < shakeMagnitude * Mathf.Clamp01(shakeTime / shakeDuration)) return;

        shakeMagnitude = magnitude;
        shakeDuration = duration;
        shakeTime = duration;
    }
}
