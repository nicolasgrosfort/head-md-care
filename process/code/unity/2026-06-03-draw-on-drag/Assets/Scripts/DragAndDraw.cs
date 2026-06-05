using UnityEngine;
using UnityEngine.InputSystem;

public class DragAndDraw : MonoBehaviour
{
    [Header("Leaf")]
    public GameObject prefabToPlace;

    [Header("Placement rules")]
    public LayerMask obstacleLayer;
    public float minSpacing = 0.1f;

    [Header("Placement")]
    public float offset = 0.05f;

    [Header("Random rotation")]
    public float minAngle = -30f;
    public float maxAngle = 30f;

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
        Vector3 spawnPos = position + normal * offset;

        // Base alignée sur la normale
        Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, normal);

        // Rotation aléatoire sur les 3 axes
        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(minAngle, maxAngle), // X
            Random.Range(0f, 360f), // Y — tour complet
            Random.Range(minAngle, maxAngle) // Z
        );

        Quaternion finalRotation = baseRotation * randomRotation;

        Instantiate(prefabToPlace, spawnPos, finalRotation);
    }
}
