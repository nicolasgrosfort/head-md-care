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

        // Reset le compteur avant de régénérer
        MossCounter.Instance?.Reset();

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

            Vector3 islandCenter = cameraVisibleOnly
                ? FindVisibleSurfacePoint(target)
                : FindSurfacePoint(target);

            if (islandCenter == Vector3.zero)
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

        // Grille de points dans le rayon de l'îlot
        int gridRes = Mathf.CeilToInt(Mathf.Sqrt(pointsPerIsland));
        float step = (islandRadius * 2f) / gridRes;

        for (int xi = 0; xi < gridRes; xi++)
        {
            for (int yi = 0; yi < gridRes; yi++)
            {
                // Position sur la grille locale à l'îlot
                float localX = -islandRadius + xi * step + Random.Range(-step * 0.3f, step * 0.3f);
                float localY = -islandRadius + yi * step + Random.Range(-step * 0.3f, step * 0.3f);

                // Plusieurs octaves de noise pour un résultat organique
                float nx = (center.x + localX + noiseOffsetX) * noiseScale;
                float ny = (center.z + localY + noiseOffsetY) * noiseScale;

                float noiseValue =
                    Mathf.PerlinNoise(nx, ny) * 0.5f
                    + Mathf.PerlinNoise(nx * 2f, ny * 2f) * 0.3f
                    + Mathf.PerlinNoise(nx * 4f, ny * 4f) * 0.2f;

                // Fade organique sur les bords (cercle doux)
                float dist = Mathf.Sqrt(localX * localX + localY * localY);
                float fade = 1f - Mathf.SmoothStep(0f, islandRadius, dist);

                if (noiseValue * fade < noiseThreshold)
                    continue;

                // Raycast depuis ce point vers la surface
                Vector3 rayOrigin = center + new Vector3(localX, islandRadius, localY);
                Ray ray = new Ray(rayOrigin, (center - rayOrigin).normalized);

                if (!Physics.Raycast(ray, out RaycastHit hit, islandRadius * 3f, obstacleLayer))
                    continue;

                if (Vector3.Distance(hit.point, center) > islandRadius * 1.2f)
                    continue;

                if (cameraVisibleOnly && !IsVisibleFromCamera(hit.point))
                    continue;

                PlacePrefab(hit.point, hit.normal);
            }
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

        MossCounter.Instance?.Register(1);
    }

    private bool IsVisibleFromCamera(Vector3 point)
    {
        // 1. Dans le frustum ?
        Vector3 viewport = _cam.WorldToViewportPoint(point);
        if (
            viewport.x < 0f
            || viewport.x > 1f
            || viewport.y < 0f
            || viewport.y > 1f
            || viewport.z <= 0f
        )
            return false;

        // 2. Rayon depuis la caméra vers le point — rien ne doit bloquer
        Vector3 direction = point - _cam.transform.position;
        float distance = direction.magnitude;

        if (
            Physics.Raycast(
                _cam.transform.position,
                direction.normalized,
                out RaycastHit hit,
                distance - 0.05f
            )
        )
            return false; // Quelque chose bloque

        return true;
    }

    private bool FindSurfacePoint(Collider col, out Vector3 point, out Vector3 normal)
    {
        Bounds b = col.bounds;
        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector3 dir = Random.onUnitSphere;
            Ray ray = new Ray(b.center + dir * b.extents.magnitude * 2f, -dir);
            if (Physics.Raycast(ray, out RaycastHit hit, b.extents.magnitude * 4f, obstacleLayer))
            {
                point = hit.point;
                normal = hit.normal;
                return true;
            }
        }
        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    private Vector3 FindVisibleSurfacePoint(Collider col)
    {
        Bounds b = col.bounds;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            // Point aléatoire dans le bounds projeté en viewport
            Vector3 randomPoint = new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z)
            );

            // Convertit ce point en position écran
            Vector3 viewport = _cam.WorldToViewportPoint(randomPoint);

            // Ignore si hors écran
            if (
                viewport.x < 0f
                || viewport.x > 1f
                || viewport.y < 0f
                || viewport.y > 1f
                || viewport.z <= 0f
            )
                continue;

            // Tire un rayon depuis la caméra vers ce point écran
            Ray ray = _cam.ViewportPointToRay(viewport);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, obstacleLayer))
            {
                // Vérifie que le hit appartient bien au bon collider
                if (hit.collider == col)
                    return hit.point;
            }
        }

        return Vector3.zero;
    }
}
