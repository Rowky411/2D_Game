using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset;

    [Header("Bounds")]
    [SerializeField] private bool clampToBounds;
    [SerializeField] private Rect bounds = new Rect(0, 0, 50, 40);

    private Vector3 velocity;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target != null)
            transform.position = GetTargetPosition();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, GetTargetPosition(), ref velocity, smoothTime);
    }

    // Keep the camera's own Z so it stays in front of the 2D scene.
    private Vector3 GetTargetPosition()
    {
        var position = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
        if (!clampToBounds || cam == null) return position;

        // Keep the view inside the map so nothing outside it is visible.
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        position.x = ClampAxis(position.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
        position.y = ClampAxis(position.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);
        return position;
    }

    // Centers the camera on an axis where the map is smaller than the view.
    private static float ClampAxis(float value, float min, float max)
    {
        return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
    }
}
