using UnityEngine;

public class MossSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject mossPrefab;

    [Header("Obstacles ciblés")]
    public LayerMask obstacleLayer;

    [Header("Îlots")]
    public int islandCount = 5; // Nombre d'îlots dans la scène
    public float islandRadius = 0.5f; // Taille de chaque îlot

    [Header("Densité")]
    public int pointsPerIsland = 30; // Prefabs par îlot
    public float noiseScale = 2f; // Grain du noise

    [Range(0f, 1f)]
    public float noiseThreshold = 0.4f; // 0 = dense, 1 = rare

    [Header("Placement")]
    public float offset = 0.02f;
    public float minAngle = -20f;
    public float maxAngle = 20f;

    [Header("Variation de taille")]
    public float minScale = 0.3f;
    public float maxScale = 0.8f;

    [Header("Caméra")]
    public bool cameraVisibleOnly = true;

    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        Spawn();
    }

    [ContextMenu("Regenerate")]
    public void Spawn()
    {
        _cam = Camera.main;

        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        Collider[] obstacles = FindObjectsByType<Collider>(FindObjectsInactive.Exclude);

        System.Collections.Generic.List<Collider> targets = new();
        foreach (Collider col in obstacles)
        {
            if ((obstacleLayer.value & (1 << col.gameObject.layer)) != 0)
                targets.Add(col);
        }

        if (targets.Count == 0)
        {
            Debug.LogWarning("MossSpawner: aucun obstacle trouvé sur le layer sélectionné.");
            return;
        }

        for (int i = 0; i < islandCount; i++)
        {
            Collider target = targets[Random.Range(0, targets.Count)];

            Vector3 islandCenter = FindSurfacePoint(target);
            if (islandCenter == Vector3.zero)
                continue;

            // Vérifie que le centre de l'îlot est visible
            if (cameraVisibleOnly && !IsVisibleFromCamera(islandCenter))
                continue;

            SpawnIsland(islandCenter, target);
        }
    }

    private Vector3 FindSurfacePoint(Collider col)
    {
        Bounds b = col.bounds;
        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector3 dir = Random.onUnitSphere;
            Ray ray = new Ray(b.center + dir * b.extents.magnitude * 2f, -dir);
            if (Physics.Raycast(ray, out RaycastHit hit, b.extents.magnitude * 4f, obstacleLayer))
                return hit.point;
        }
        return Vector3.zero;
    }

    private void SpawnIsland(Vector3 center, Collider col)
    {
        float noiseOffsetX = Random.Range(0f, 999f);
        float noiseOffsetY = Random.Range(0f, 999f);

        for (int i = 0; i < pointsPerIsland; i++)
        {
            // Point aléatoire dans la sphère de l'îlot
            Vector3 randomOffset = Random.insideUnitSphere * islandRadius;
            Vector3 rayOrigin = center + randomOffset;

            // Tire vers le centre de l'îlot pour coller à la surface
            Ray ray = new Ray(
                rayOrigin + (center - rayOrigin).normalized * -islandRadius,
                (center - rayOrigin).normalized
            );

            if (!Physics.Raycast(ray, out RaycastHit hit, islandRadius * 3f, obstacleLayer))
                continue;

            // Doit rester dans le rayon de l'îlot
            if (Vector3.Distance(hit.point, center) > islandRadius)
                continue;

            // Perlin noise pour la forme organique
            float nx = (hit.point.x + noiseOffsetX) * noiseScale;
            float ny = (hit.point.y + noiseOffsetY) * noiseScale;
            float noiseValue = Mathf.PerlinNoise(nx, ny);

            // Fade sur les bords : plus on est loin du centre, plus c'est rare
            float distanceFade = 1f - (Vector3.Distance(hit.point, center) / islandRadius);
            if (noiseValue * distanceFade < noiseThreshold)
                continue;

            PlacePrefab(hit.point, hit.normal);
        }
    }

    private void PlacePrefab(Vector3 position, Vector3 normal)
    {
        Vector3 spawnPos = position + normal * offset;

        Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, normal);
        Quaternion randomSpin = Quaternion.Euler(
            Random.Range(minAngle, maxAngle),
            Random.Range(0f, 360f),
            Random.Range(minAngle, maxAngle)
        );

        float scale = Random.Range(minScale, maxScale);
        GameObject go = Instantiate(mossPrefab, spawnPos, baseRotation * randomSpin, transform);
        go.transform.localScale = Vector3.one * scale;
    }

    private bool IsVisibleFromCamera(Vector3 point)
    {
        Vector3 viewport = _cam.WorldToViewportPoint(point);
        return viewport.x >= 0f
            && viewport.x <= 1f
            && viewport.y >= 0f
            && viewport.y <= 1f
            && viewport.z > 0f;
    }
}
