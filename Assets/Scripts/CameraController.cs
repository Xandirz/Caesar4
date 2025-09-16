using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

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