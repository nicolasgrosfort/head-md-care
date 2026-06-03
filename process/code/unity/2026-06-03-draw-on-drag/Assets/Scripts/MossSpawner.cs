using UnityEngine;

public class MossSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject mossPrefab;

    [Header("Obstacles ciblés")]
    public LayerMask obstacleLayer;

    [Header("Zones de mousse")]
    public int zoneCount = 5; // Nombre de zones
    public float zoneRadius = 1.5f; // Rayon de chaque zone

    [Header("Densité")]
    public int pointsPerZone = 20; // Prefabs par zone
    public float noiseScale = 1.5f; // Echelle du noise (+ grand = + étalé)
    public float noiseThreshold = 0.5f; // Seuil : 0 = dense, 1 = rare

    [Header("Placement")]
    public float offset = 0.02f;
    public float minAngle = -20f;
    public float maxAngle = 20f;

    [Header("Variation de taille")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    void Start()
    {
        Spawn();
    }

    [ContextMenu("Regenerate")]
    public void Spawn()
    {
        // Nettoie les anciens
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        // Trouve tous les colliders sur le layer obstacle
        Collider[] obstacles = FindObjectsByType<Collider>(FindObjectsInactive.Exclude);

        foreach (Collider col in obstacles)
        {
            if ((obstacleLayer.value & (1 << col.gameObject.layer)) == 0)
                continue;

            SpawnOnCollider(col);
        }
    }

    private void SpawnOnCollider(Collider col)
    {
        Bounds bounds = col.bounds;

        for (int z = 0; z < zoneCount; z++)
        {
            float noiseOffsetX = Random.Range(0f, 999f);
            float noiseOffsetZ = Random.Range(0f, 999f);

            for (int i = 0; i < pointsPerZone; i++)
            {
                // Direction aléatoire sur une sphère
                Vector3 randomDir = Random.onUnitSphere;

                // Origine : depuis l'extérieur du bounds dans cette direction
                Vector3 rayOrigin = bounds.center + randomDir * bounds.extents.magnitude * 2f;

                // Tire vers le centre
                Ray ray = new Ray(rayOrigin, -randomDir);

                if (
                    !Physics.Raycast(
                        ray,
                        out RaycastHit hit,
                        bounds.extents.magnitude * 4f,
                        obstacleLayer
                    )
                )
                    continue;

                // Perlin noise basé sur la position du hit
                float nx = (hit.point.x + noiseOffsetX) * noiseScale;
                float nz = (hit.point.z + noiseOffsetZ) * noiseScale;
                float noiseValue = Mathf.PerlinNoise(nx, nz);

                if (noiseValue < noiseThreshold)
                    continue;

                // Vérifie qu'on est dans le rayon de la zone
                Vector3 zoneCenter = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z)
                );

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
        Quaternion finalRotation = baseRotation * randomSpin;

        float scale = Random.Range(minScale, maxScale);

        GameObject go = Instantiate(mossPrefab, spawnPos, finalRotation, transform);
        go.transform.localScale = Vector3.one * scale;
    }
}
