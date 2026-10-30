using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// Единственный игрок на сцене. Вместо поиска через Find* остальные скрипты берут его отсюда.
    /// Выставляется в Awake, поэтому доступен уже в Start любого другого скрипта.
    /// </summary>
    public static PlayerController Instance { get; private set; }

    [Header("Движение")]
    [SerializeField] private float moveSpeed = 7f;
    [Tooltip("Если включено, W/A/S/D двигают персонажа относительно направления камеры, а не осей мира.")]
    [SerializeField] private bool cameraRelative = true;

    [Header("Рывок")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.18f;
    [Tooltip("Время от начала одного рывка до начала следующего (сек).")]
    [SerializeField] private float dashCooldown = 0.8f;

    private Rigidbody rb;
    private Camera cam;
    private Vector3 moveInput;
    private Vector3 lastMoveDirection;
    private Vector3 dashDirection;
    private bool dashRequested;
    private bool isDashing;
    private float dashEndTime;
    private float nextDashTime;

    public Health PlayerHealth { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("На сцене больше одного PlayerController, Instance указывает на последнего.", this);
        }
        Instance = this;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        PlayerHealth = GetComponent<Health>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        dashSpeed = Mathf.Max(0f, dashSpeed);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashCooldown = Mathf.Max(dashDuration, dashCooldown);
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveInput = GetMoveDirection(x, z);

        if (moveInput.sqrMagnitude > 0.01f) lastMoveDirection = moveInput;

        // Нажатие запоминаем здесь, а выполняем в FixedUpdate вместе со всей физикой.
        // На паузе (timeScale = 0) рывок не запоминаем, чтобы он не сработал после снятия паузы.
        if (Time.timeScale > 0f && Input.GetKeyDown(dashKey))
        {
            dashRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (dashRequested)
        {
            dashRequested = false;
            if (!isDashing && Time.time >= nextDashTime) StartDash();
        }

        if (isDashing && Time.time >= dashEndTime) isDashing = false;

        Vector3 horizontal = isDashing ? dashDirection * dashSpeed : moveInput * moveSpeed;

        // Вертикальную скорость не трогаем, гравитация работает как обычно
        Vector3 velocity = rb.linearVelocity;
        velocity.x = horizontal.x;
        velocity.z = horizontal.z;
        rb.linearVelocity = velocity;
    }

    private void StartDash()
    {
        // Рывок идёт туда, куда игрок сейчас направляет персонажа.
        // Если стоит на месте, то в последнюю сторону движения, а если ещё не двигался, то вперёд от камеры.
        Vector3 direction = moveInput.sqrMagnitude > 0.01f ? moveInput : lastMoveDirection;
        if (direction.sqrMagnitude < 0.01f) direction = GetMoveDirection(0f, 1f);
        if (direction.sqrMagnitude < 0.01f) direction = transform.forward;

        dashDirection = direction.normalized;
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
    }

    private Vector3 GetMoveDirection(float x, float z)
    {
        if (!cameraRelative)
        {
            return new Vector3(x, 0f, z).normalized;
        }

        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            return new Vector3(x, 0f, z).normalized;
        }

        // Берём направления камеры, спроецированные на горизонтальную плоскость
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = cam.transform.right;
        right.y = 0f;
        right.Normalize();

        return (right * x + forward * z).normalized;
    }
}
