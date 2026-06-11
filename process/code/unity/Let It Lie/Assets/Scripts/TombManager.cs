using System.Collections.Generic;
using UnityEngine;

public class TombManager : MonoBehaviour
{
    [Header("Prefab à spawner")]
    public GameObject prefabToSpread;

    [Header("Quantité")]
    public int maxCount = 50;

    [Header("Perlin Noise — Densité")]
    public float noiseScale = 3f; // fréquence du bruit (plus grand = zones plus petites)
    public float noiseThreshold = 0.5f; // seuil : 0 = tout spawne, 1 = presque rien
    public Vector2 noiseOffset; // offset aléatoire au Start pour varier entre instances

    [Header("Scale aléatoire")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    [Header("Rotation Y aléatoire")]
    public bool randomRotationY = true;

    [Header("Options")]
    public bool spawnOnStart = true;
    public bool snapToSurface = true; // Raycast pour coller à la surface du mesh

    private MeshRenderer _meshRenderer;
    private readonly List<GameObject> _spawned = new();

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();

        if (_meshRenderer == null)
        {
            Debug.LogError("[PrefabSpreader] Aucun MeshRenderer trouvé sur ce GameObject.");
            return;
        }

        // Offset aléatoire pour que chaque instance ait un pattern différent
        noiseOffset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));

        if (spawnOnStart)
            Spread();
    }

    public void Spread()
    {
        Clear();

        if (prefabToSpread == null)
        {
            Debug.LogWarning("[PrefabSpreader] Assigne un prefab !");
            return;
        }

        Bounds bounds = _meshRenderer.bounds; // bounds en world space

        float width = bounds.size.x;
        float depth = bounds.size.z;
        float surfaceY = bounds.max.y; // dessus du mesh

        int attempts = maxCount * 8;
        int spawned = 0;

        for (int i = 0; i < attempts && spawned < maxCount; i++)
        {
            // Position aléatoire dans les bounds XZ
            float rx = Random.Range(bounds.min.x, bounds.max.x);
            float rz = Random.Range(bounds.min.z, bounds.max.z);

            // Coordonnées normalisées [0,1] pour le bruit
            float nx = (rx - bounds.min.x) / width;
            float nz = (rz - bounds.min.z) / depth;

            // Échantillon Perlin noise
            float noise = Mathf.PerlinNoise(
                noiseOffset.x + nx * noiseScale,
                noiseOffset.y + nz * noiseScale
            );

            // Rejeter si en dessous du seuil
            if (noise < noiseThreshold)
                continue;

            // Position Y : raycast depuis au-dessus pour coller à la surface
            Vector3 spawnPos = new Vector3(rx, surfaceY, rz);

            if (snapToSurface)
            {
                Ray ray = new Ray(new Vector3(rx, surfaceY + 1f, rz), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                    spawnPos.y = hit.point.y;
            }

            // Instancier
            GameObject go = Instantiate(prefabToSpread, spawnPos, Quaternion.identity, transform);

            if (randomRotationY)
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float s = Random.Range(minScale, maxScale);
            go.transform.localScale = Vector3.one * s;

            _spawned.Add(go);
            spawned++;
        }

        Debug.Log($"[PrefabSpreader] {spawned} objets placés.");
    }

    public void Clear()
    {
        foreach (var go in _spawned)
            if (go != null)
                Destroy(go);
        _spawned.Clear();
    }

#if UNITY_EDITOR
    // Visualisation des bounds et du noise dans la scène
    void OnDrawGizmosSelected()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr == null)
            return;

        Bounds b = mr.bounds;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.3f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}
