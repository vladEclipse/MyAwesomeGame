using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.12f;

    [Tooltip("Если включено, смещение камеры относительно цели берётся из текущего положения в сцене при старте")]
    [SerializeField] private bool useInitialOffset = true;
    [SerializeField] private Vector3 offset = new Vector3(15f, 10f, 15f);

    private Vector3 velocity;

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
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
