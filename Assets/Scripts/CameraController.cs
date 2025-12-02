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

    [Header("UI")]
    [Tooltip("RectTransform объекта ResearchTree (панель дерева исследований)")]
    [SerializeField] private RectTransform researchTreeRect;

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

        // ───── Блокируем DRAG камеры, если мышь над панелью ResearchTree ─────
        bool mouseOverResearchTree = IsMouseOverResearchTree();

        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2) && !mouseOverResearchTree)
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
            // ❗ Если мышка над ResearchTree и он активен — НЕ зумим камеру
            if (IsMouseOverResearchTree())
                return;

            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    // ───── Проверка, что мышка над панелью ResearchTree ─────
    private bool IsMouseOverResearchTree()
    {
        if (researchTreeRect == null)
            return false;

        // если дерево скрыто — не считаем, что мышь "над ним"
        if (!researchTreeRect.gameObject.activeInHierarchy)
            return false;

        // Canvas в режиме Screen Space Overlay → камеру можно передать null
        return RectTransformUtility.RectangleContainsScreenPoint(
            researchTreeRect,
            Input.mousePosition,
            null
        );
    }
}
