// IEnumerator import ?

using System.Collections;
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

    [Header("Repousse")]
    public bool autoRegrow = true;
    public float regrowDelay = 5f;
    public float timeBetweenPrefabs = 0.05f;

    [Header("Accélération")]
    public float delayReduction = 0.5f; // secondes retirées à chaque repousse
    public float minRegrowDelay = 0.5f; // délai minimum
    public float speedMultiplier = 1.5f; // multiplie la vitesse à chaque repousse
    public float maxSpeedMultiplier = 10f; // vitesse maximum

    private float _currentDelay;
    private float _currentSpeed;
    private float _currentTimeBetween;

    [Header("Caméra")]
    public bool cameraVisibleOnly = true;

    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        _currentDelay = regrowDelay;
        _currentTimeBetween = timeBetweenPrefabs;
        Spawn();
        if (autoRegrow)
            StartCoroutine(RegrowRoutine());
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

    public void OnMossErased()
    {
        // Réduit le délai
        _currentDelay = Mathf.Max(minRegrowDelay, _currentDelay - delayReduction);

        // Accélère la vitesse progressive
        _currentTimeBetween = Mathf.Max(
            timeBetweenPrefabs / maxSpeedMultiplier,
            _currentTimeBetween / speedMultiplier
        );

        Debug.Log(
            $"Repousse accélérée — délai: {_currentDelay:F1}s, vitesse: {_currentTimeBetween:F3}s"
        );
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

        // Normale approximative au centre de l'îlot
        Vector3 normal = GetSurfaceNormal(center, col);

        // Deux axes perpendiculaires à la normale = plan local de la surface
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.01f)
            tangent = Vector3.Cross(normal, Vector3.forward);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

        int gridRes = Mathf.CeilToInt(Mathf.Sqrt(pointsPerIsland));
        float step = (islandRadius * 2f) / gridRes;

        for (int xi = 0; xi < gridRes; xi++)
        {
            for (int yi = 0; yi < gridRes; yi++)
            {
                float localX = -islandRadius + xi * step + Random.Range(-step * 0.4f, step * 0.4f);
                float localY = -islandRadius + yi * step + Random.Range(-step * 0.4f, step * 0.4f);

                // Point dans le plan de la surface
                Vector3 samplePoint = center + tangent * localX + bitangent * localY;

                // Noise multi-octaves
                float nx = (samplePoint.x + noiseOffsetX) * noiseScale;
                float ny = (samplePoint.z + noiseOffsetY) * noiseScale;

                float noiseValue =
                    Mathf.PerlinNoise(nx, ny) * 0.5f
                    + Mathf.PerlinNoise(nx * 2.1f, ny * 2.1f) * 0.3f
                    + Mathf.PerlinNoise(nx * 4.3f, ny * 4.3f) * 0.2f;

                // Bord doux
                float dist = Mathf.Sqrt(localX * localX + localY * localY);
                float fade = 1f - Mathf.SmoothStep(0f, islandRadius, dist);

                if (noiseValue * fade < noiseThreshold)
                    continue;

                // Raycast depuis le point + normale vers la surface
                Ray ray = new Ray(samplePoint + normal * islandRadius, -normal);

                if (!Physics.Raycast(ray, out RaycastHit hit, islandRadius * 2f, obstacleLayer))
                    continue;

                if (Vector3.Distance(hit.point, center) > islandRadius * 1.5f)
                    continue;

                if (cameraVisibleOnly && !IsVisibleFromCamera(hit.point))
                    continue;

                PlacePrefab(hit.point, hit.normal);
            }
        }
    }

    private Vector3 GetSurfaceNormal(Vector3 point, Collider col)
    {
        Bounds b = col.bounds;
        Vector3 dir = (point - b.center).normalized;
        Ray ray = new Ray(b.center + dir * b.extents.magnitude * 2f, -dir);

        if (Physics.Raycast(ray, out RaycastHit hit, b.extents.magnitude * 4f, obstacleLayer))
            return hit.normal;

        return Vector3.up;
    }

    private void PlacePrefab(Vector3 position, Vector3 normal, bool isRegrow = false)
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

        if (isRegrow)
            MossCounter.Instance?.Regrow(1); // remonte le Remaining sans toucher au Total
        else
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

    private IEnumerator RegrowRoutine()
    {
        while (true)
        {
            yield return new WaitUntil(() =>
                MossCounter.Instance != null
                && MossCounter.Instance.Remaining < MossCounter.Instance.Total
            );

            yield return new WaitForSeconds(_currentDelay);

            int missing = MossCounter.Instance.Total - MossCounter.Instance.Remaining;
            yield return StartCoroutine(RegrowMissing(missing));
        }
    }

    private IEnumerator RegrowMissing(int count)
    {
        Collider[] obstacles = FindObjectsByType<Collider>(FindObjectsInactive.Exclude);
        System.Collections.Generic.List<Collider> targets = new();
        foreach (Collider col in obstacles)
            if ((obstacleLayer.value & (1 << col.gameObject.layer)) != 0)
                targets.Add(col);

        if (targets.Count == 0)
            yield break;

        int regrown = 0;

        while (regrown < count)
        {
            Collider target = targets[Random.Range(0, targets.Count)];
            Vector3 center = cameraVisibleOnly
                ? FindVisibleSurfacePoint(target)
                : FindSurfacePoint(target);

            if (center == Vector3.zero)
                continue;

            foreach (var point in GetIslandPoints(center, target))
            {
                if (regrown >= count)
                    yield break;

                PlacePrefab(point.position, point.normal, isRegrow: true); // ← true ici
                regrown++;
                yield return new WaitForSeconds(_currentTimeBetween);
            }
        }
    }

    private IEnumerator SpawnProgressive()
    {
        Collider[] obstacles = FindObjectsByType<Collider>(FindObjectsInactive.Exclude);
        System.Collections.Generic.List<Collider> targets = new();
        foreach (Collider col in obstacles)
            if ((obstacleLayer.value & (1 << col.gameObject.layer)) != 0)
                targets.Add(col);

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = islandCount * 50;

        while (spawned < islandCount && attempts < maxAttempts)
        {
            attempts++;
            Collider target = targets[Random.Range(0, targets.Count)];
            Vector3 center = cameraVisibleOnly
                ? FindVisibleSurfacePoint(target)
                : FindSurfacePoint(target);

            if (center == Vector3.zero)
                continue;

            // Place prefab par prefab avec pause
            int placed = 0;
            foreach (var point in GetIslandPoints(center, target))
            {
                PlacePrefab(point.position, point.normal, isRegrow: true);
                placed++;
                yield return new WaitForSeconds(timeBetweenPrefabs);
            }

            if (placed > 0)
                spawned++;
        }
    }

    // Structure pour retourner position + normale
    private struct SurfacePoint
    {
        public Vector3 position,
            normal;
    }

    private System.Collections.Generic.IEnumerable<SurfacePoint> GetIslandPoints(
        Vector3 center,
        Collider col
    )
    {
        float noiseOffsetX = Random.Range(0f, 999f);
        float noiseOffsetY = Random.Range(0f, 999f);

        Vector3 normal = GetSurfaceNormal(center, col);
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.01f)
            tangent = Vector3.Cross(normal, Vector3.forward);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

        int gridRes = Mathf.CeilToInt(Mathf.Sqrt(pointsPerIsland));
        float step = (islandRadius * 2f) / gridRes;

        for (int xi = 0; xi < gridRes; xi++)
        {
            for (int yi = 0; yi < gridRes; yi++)
            {
                float localX = -islandRadius + xi * step + Random.Range(-step * 0.4f, step * 0.4f);
                float localY = -islandRadius + yi * step + Random.Range(-step * 0.4f, step * 0.4f);

                Vector3 samplePoint = center + tangent * localX + bitangent * localY;

                float nx = (samplePoint.x + noiseOffsetX) * noiseScale;
                float ny = (samplePoint.z + noiseOffsetY) * noiseScale;

                float noiseValue =
                    Mathf.PerlinNoise(nx, ny) * 0.5f
                    + Mathf.PerlinNoise(nx * 2.1f, ny * 2.1f) * 0.3f
                    + Mathf.PerlinNoise(nx * 4.3f, ny * 4.3f) * 0.2f;

                float dist = Mathf.Sqrt(localX * localX + localY * localY);
                float fade = 1f - Mathf.SmoothStep(0f, islandRadius, dist);

                if (noiseValue * fade < noiseThreshold)
                    continue;

                Ray ray = new Ray(samplePoint + normal * islandRadius, -normal);
                if (!Physics.Raycast(ray, out RaycastHit hit, islandRadius * 2f, obstacleLayer))
                    continue;
                if (Vector3.Distance(hit.point, center) > islandRadius * 1.5f)
                    continue;
                if (cameraVisibleOnly && !IsVisibleFromCamera(hit.point))
                    continue;

                yield return new SurfacePoint { position = hit.point, normal = hit.normal };
            }
        }
    }
}
