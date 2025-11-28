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

        bool mouseOverResearchTree = IsMouseOverResearchTree();

        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
        }

        // двигаем камеру средней кнопкой ТОЛЬКО если мышь НЕ над активной панелью ресёрча
        if (Input.GetMouseButton(2) && !mouseOverResearchTree)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * (moveSpeed * Time.deltaTime);
            transform.Translate(move, Space.World);

            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(h, v, 0f).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    // === мышь над АКТИВНОЙ панелью ResearchTree ===
    private bool IsMouseOverResearchTree()
    {
        // если ссылка не задана или панель не активна в иерархии — НЕ блокируем камеру
        if (researchTreeRect == null || !researchTreeRect.gameObject.activeInHierarchy)
            return false;

        // Canvas в Screen Space Overlay → камеру можно передать null
        return RectTransformUtility.RectangleContainsScreenPoint(
            researchTreeRect,
            Input.mousePosition,
            null
        );
    }
}
