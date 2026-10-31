using UnityEngine;

/// <summary>
/// Управление игроком: ходьба относительно камеры, рывок, поворот модели,
/// движение по склонам и подъём на невысокие уступы.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// Единственный игрок на сцене. Вместо поиска через Find* остальные скрипты берут его отсюда.
    /// Выставляется в Awake, поэтому доступен уже в Start любого другого скрипта.
    /// </summary>
    public static PlayerController Instance { get; private set; }

    [Header("Движение")]
    [Min(0f)] [SerializeField] private float moveSpeed = 7f;
    [Tooltip("Если включено, W/A/S/D двигают персонажа относительно направления камеры, а не осей мира.")]
    [SerializeField] private bool cameraRelative = true;

    [Header("Рывок")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [Min(0f)] [SerializeField] private float dashSpeed = 22f;
    [Min(0.01f)] [SerializeField] private float dashDuration = 0.18f;
    [Tooltip("Время от начала одного рывка до начала следующего (сек).")]
    [Min(0.01f)] [SerializeField] private float dashCooldown = 0.8f;

    [Header("Поворот")]
    [Tooltip("Скорость поворота модели в сторону движения (градусов в секунду).")]
    [Min(0f)] [SerializeField] private float turnSpeed = 720f;

    [Header("Рельеф")]
    [Tooltip("На какую высоту персонаж сам взбирается на уступы (мостики, блоки, поребрики).")]
    [Min(0f)] [SerializeField] private float stepHeight = 0.55f;
    [Tooltip("Самый крутой наклон (градусы), по которому можно подниматься.")]
    [Range(0f, 85f)] [SerializeField] private float maxSlopeAngle = 60f;

    private Rigidbody rb;
    private Collider body;
    private Camera cam;
    // Готовый массив для NonAlloc-лучей: не создаём новый на каждый луч
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private Vector3 moveInput;
    private Vector3 lastMoveDirection; // для рывка с места
    private bool dashRequested;
    private float dashEndTime;
    private float nextDashTime;

    public Health PlayerHealth { get; private set; }
    public bool IsDashing { get; private set; }
    public Vector3 DashDirection { get; private set; }

    /// Вызывается в момент начала рывка, аргумент - направление (для звуков и эффектов)
    public event System.Action<Vector3> Dashed;

    private void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody>();
        body = GetComponent<Collider>();
        rb.freezeRotation = true; // поворачиваем персонажа только сами, физика его не опрокидывает
        PlayerHealth = GetComponent<Health>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Включает или выключает управление игроком (движение и атаку). При выключении персонаж останавливается.</summary>
    public void SetControlEnabled(bool value)
    {
        enabled = value;
        if (TryGetComponent(out PlayerAttack attack)) attack.enabled = value;
        if (!value) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private void Update()
    {
        moveInput = GetMoveDirection(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (moveInput.sqrMagnitude > 0.01f) lastMoveDirection = moveInput;

        // Нажатие запоминаем здесь, а выполняем в FixedUpdate вместе со всей физикой:
        // GetKeyDown верен только один кадр, и FixedUpdate мог бы его пропустить.
        // На паузе (timeScale = 0) рывок не запоминаем, чтобы он не сработал после снятия паузы.
        if (Time.timeScale > 0f && Input.GetKeyDown(dashKey)) dashRequested = true;
    }

    // Физика: вызывается с фиксированным шагом, здесь меняем скорость Rigidbody
    private void FixedUpdate()
    {
        // Рывок во время отката или другого рывка просто игнорируется
        if (dashRequested)
        {
            dashRequested = false;
            if (!IsDashing && Time.time >= nextDashTime) StartDash();
        }

        if (IsDashing && Time.time >= dashEndTime) IsDashing = false;

        // Во время рывка направление фиксировано, WASD на него не влияет
        Vector3 horizontal = IsDashing ? DashDirection * dashSpeed : moveInput * moveSpeed;
        bool moving = horizontal.sqrMagnitude > 0.0001f;

        Bounds b = body.bounds;
        float radius = Mathf.Max(0.1f, Mathf.Min(b.extents.x, b.extents.z));

        bool grounded = FindGround(b, radius, out Vector3 groundNormal) &&
                        Vector3.Angle(groundNormal, Vector3.up) <= maxSlopeAngle;

        if (moving) TryStepUp(horizontal.normalized, b, radius);

        if (!grounded)
        {
            // В воздухе вертикальную скорость не трогаем, гравитация работает как обычно
            rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
        }
        else if (moving)
        {
            // Движемся вдоль поверхности: вверх по арке мостика и вниз без подскоков
            rb.linearVelocity = Vector3.ProjectOnPlane(horizontal, groundNormal).normalized * horizontal.magnitude;
        }
        else
        {
            // Стоим на месте на наклоне: не скатываемся
            rb.linearVelocity = Vector3.zero;
        }

        // Персонаж смотрит туда, куда движется (во время рывка - по направлению рывка)
        Vector3 facing = IsDashing ? DashDirection : moveInput;
        if (facing.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(facing, Vector3.up);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, target, turnSpeed * Time.fixedDeltaTime);
        }
    }

    // Поверхность под ногами: сфера вниз от низа персонажа
    private bool FindGround(Bounds b, float radius, out Vector3 normal)
    {
        Vector3 origin = new Vector3(b.center.x, b.min.y + radius + 0.05f, b.center.z);

        bool found = CastWorld(origin, Vector3.down, 0.4f, radius * 0.9f, out RaycastHit hit);
        normal = found ? hit.normal : Vector3.up;
        return found;
    }

    // Взбираемся на небольшой уступ: низкий луч упирается в стенку, а на высоте шага путь свободен
    private void TryStepUp(Vector3 direction, Bounds b, float radius)
    {
        float feet = b.min.y;
        float reach = radius + 0.2f;

        Vector3 low = new Vector3(b.center.x, feet + 0.08f, b.center.z);
        if (!CastWorld(low, direction, reach, 0f, out RaycastHit wall)) return;

        // Пологий наклон мы проходим без шага
        if (Vector3.Angle(wall.normal, Vector3.up) <= maxSlopeAngle) return;

        Vector3 high = new Vector3(b.center.x, feet + stepHeight + 0.05f, b.center.z);
        if (CastWorld(high, direction, reach, 0f, out _)) return; // стена выше шага - не залезть

        // Находим верх уступа и поднимаем персонажа на него
        Vector3 ahead = high + direction * reach;
        if (!CastWorld(ahead, Vector3.down, stepHeight + 0.1f, 0f, out RaycastHit top)) return;

        float rise = top.point.y - feet;
        if (rise > 0.02f && rise <= stepHeight + 0.02f) rb.position += Vector3.up * (rise + 0.02f);
    }

    // Луч или сфера по миру (игрок и мобы на слое Actors сюда не попадают). Возвращает ближайшее попадание.
    private bool CastWorld(Vector3 origin, Vector3 direction, float distance, float radius, out RaycastHit best)
    {
        int count = radius > 0f
            ? Physics.SphereCastNonAlloc(origin, radius, direction, hitBuffer, distance, Layers.World, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance, Layers.World, QueryTriggerInteraction.Ignore);

        best = default;
        bool found = false;

        // NonAlloc не сортирует попадания, поэтому ближайшее ищем сами
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (radius > 0f && hit.distance <= 0f) continue; // сфера уже пересекает объект в начале

            if (!found || hit.distance < best.distance)
            {
                best = hit;
                found = true;
            }
        }

        return found;
    }

    private void StartDash()
    {
        // Рывок идёт туда, куда игрок сейчас направляет персонажа.
        // Если стоит на месте, то в последнюю сторону движения, а если ещё не двигался, то вперёд от камеры.
        Vector3 direction = moveInput.sqrMagnitude > 0.01f ? moveInput : lastMoveDirection;
        if (direction.sqrMagnitude < 0.01f) direction = GetMoveDirection(0f, 1f);
        if (direction.sqrMagnitude < 0.01f) direction = transform.forward;

        DashDirection = direction.normalized;
        IsDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        Dashed?.Invoke(DashDirection);
    }

    private Vector3 GetMoveDirection(float x, float z)
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraRelative)
        {
            if (cam == null) cam = Camera.main;

            // Направления камеры, спроецированные на горизонтальную плоскость
            if (cam != null)
            {
                forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            }
        }

        // normalized: по диагонали персонаж не бегает быстрее
        return (right * x + forward * z).normalized;
    }
}
