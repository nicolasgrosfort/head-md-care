using UnityEngine;
using UnityEngine.InputSystem;

public class DragAndDraw : MonoBehaviour
{
    public GameObject prefabToPlace;
    public LayerMask obstacleLayer;
    public float minSpacing = 0.1f;

    private Camera _cam;
    private Vector3 _lastPlacedPosition;

    void Awake()
    {
        _cam = Camera.main;
        _lastPlacedPosition = Vector3.positiveInfinity;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.isPressed)
        {
            TryPlace(mouse.position.ReadValue());
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            _lastPlacedPosition = Vector3.positiveInfinity;
        }
    }

    private void TryPlace(Vector2 screenPos)
    {
        Ray ray = _cam.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, obstacleLayer))
        {
            Debug.Log("No hit");
            return;
        }

        if (Vector3.Distance(hit.point, _lastPlacedPosition) < minSpacing)
            return;

        PlacePrefab(hit.point, hit.normal);
        _lastPlacedPosition = hit.point;
    }

    private void PlacePrefab(Vector3 position, Vector3 normal)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        Instantiate(prefabToPlace, position, rotation);
    }
}
