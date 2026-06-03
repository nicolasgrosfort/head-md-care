using UnityEngine;
using UnityEngine.InputSystem;

public class MossEraser : MonoBehaviour
{
    [Header("Layer des prefabs effaçables")]
    public LayerMask erasableLayer;

    [Header("Rayon d'effacement")]
    public float eraseRadius = 0.01f;

    private Camera _cam;
    private MossSpawner _spawner;

    void Awake()
    {
        _cam = Camera.main;
        _spawner = FindObjectsByType<MossSpawner>(FindObjectsInactive.Exclude)[0];
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.isPressed)
            TryErase(mouse.position.ReadValue());
    }

    private void TryErase(Vector2 screenPos)
    {
        Ray ray = _cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, erasableLayer))
            return;

        Collider[] colliders = Physics.OverlapSphere(hit.point, eraseRadius, erasableLayer);
        foreach (Collider col in colliders)
        {
            MossCounter.Instance?.Remove(1);
            Destroy(col.gameObject);
        }

        if (colliders.Length > 0)
            _spawner?.OnMossErased();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, eraseRadius);
    }
}
