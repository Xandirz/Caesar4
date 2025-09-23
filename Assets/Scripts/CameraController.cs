using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    private Vector3 lastMousePosition;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogWarning("CameraController: Камера должна быть Orthographic для изометрии!");
        }
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        
        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            // Двигаем камеру в противоположную сторону движения мыши
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * (moveSpeed * Time.deltaTime);
            transform.Translate(move, Space.World);

            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D или стрелки
        float v = Input.GetAxisRaw("Vertical");   // W/S или стрелки

        Vector3 dir = new Vector3(h, v, 0f).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // колёсико мыши
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}