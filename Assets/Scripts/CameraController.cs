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

    [Header("Bounds")]
    [Tooltip("Максимальное расстояние камеры от стартовой позиции")]
    [SerializeField] private float maxDistanceFromStart = 10f;

    private Vector3 startPosition;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogWarning("CameraController: Камера должна быть Orthographic для изометрии!");
        }

        // Запоминаем стартовую позицию камеры
        startPosition = transform.position;
    }

    void Update()
    {
        bool researchTreeOpen = IsResearchTreeOpen();

        // Если дерево исследований ОТКРЫТО — камеру не трогаем вообще
        if (researchTreeOpen)
        {
            return;
        }

        // Дерево закрыто → двигаем камеру и зумим её
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

            // Ограничиваем радиус от стартовой позиции
            ClampToRadius();

            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D или стрелки
        float v = Input.GetAxisRaw("Vertical");   // W/S или стрелки

        Vector3 dir = new Vector3(h, v, 0f).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        // Ограничиваем радиус и для движения с клавиатуры
        ClampToRadius();
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

    // Ограничение позиции камеры радиусом от стартовой точки
    private void ClampToRadius()
    {
        // Вектор от стартовой позиции до текущей
        Vector3 offset = transform.position - startPosition;

        // Используем sqrMagnitude, чтобы избежать лишнего sqrt
        float maxDistSqr = maxDistanceFromStart * maxDistanceFromStart;
        if (offset.sqrMagnitude > maxDistSqr)
        {
            offset = offset.normalized * maxDistanceFromStart;
            transform.position = startPosition + offset;
        }
    }

    // Проверяем только факт "открыто/закрыто" дерева
    private bool IsResearchTreeOpen()
    {
        return researchTreeRect != null && researchTreeRect.gameObject.activeInHierarchy;
    }
}
