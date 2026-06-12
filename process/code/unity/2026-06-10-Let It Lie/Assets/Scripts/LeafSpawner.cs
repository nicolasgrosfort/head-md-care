using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafSpawner : MonoBehaviour
{
    // ── Paramètres Inspector ──────────────────────────────────────────────────

    [Header("Références")]
    [Tooltip("Prefab feuille (doit avoir LeafBehaviour, Rigidbody, BoxCollider).")]
    public GameObject leafPrefab;
    public GameState gameState;

    [Header("Pool")]
    [Tooltip("Nombre total de feuilles pré-instanciées.")]
    [Min(1)]
    public int poolSize = 1000;

    [Header("Spawn")]
    [Tooltip("Nombre de feuilles à spawner par vague.")]
    [Min(1)]
    public int leavesPerBatch = 50;

    [Tooltip("Délai entre chaque vague (secondes).")]
    [Min(0.016f)]
    public float batchInterval = 5f;

    [Tooltip("Surface horizontale dans laquelle les feuilles apparaissent (X et Z).")]
    public Vector2 spawnAreaSize = new Vector2(10f, 10f);

    [Tooltip("Hauteur de spawn relative au GameObject.")]
    [Min(0f)]
    public float spawnHeight = 8f;

    [Tooltip("Si true, les feuilles posées restent visibles au lieu d'être recyclées.")]
    public bool keepRestedLeaves = false;

    [Tooltip("Intervalle minimum entre vagues quand timeSpeed est au max.")]
    [Min(0.016f)]
    public float minBatchInterval = 0.5f;

    private float _spawnTimer = 0f;

    // ── Pool interne ──────────────────────────────────────────────────────────

    private readonly Queue<LeafBehaviour> _available = new Queue<LeafBehaviour>();

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        if (leafPrefab == null)
        {
            Debug.LogError("[LeafSpawner] leafPrefab non assigné.", this);
            return;
        }

        BuildPool();
    }

    private void Update()
    {
        float t = Mathf.InverseLerp(
            gameState.defaultTimeSpeed,
            gameState.maxTimeSpeed,
            gameState.timeSpeed
        );
        float interval = Mathf.Lerp(batchInterval, minBatchInterval, t);

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= interval)
        {
            _spawnTimer = 0f;
            int batch = Mathf.Min(leavesPerBatch, _available.Count);
            for (int i = 0; i < batch; i++)
                SpawnOne();
        }
    }

    // ── Pool ──────────────────────────────────────────────────────────────────

    private void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(leafPrefab, transform);
            var leaf = go.GetComponent<LeafBehaviour>();

            if (leaf == null)
            {
                Debug.LogError("[LeafSpawner] Le prefab n'a pas de composant LeafBehaviour.", go);
                Destroy(go);
                continue;
            }

            go.SetActive(false);
            _available.Enqueue(leaf);
        }
    }

    private LeafBehaviour GetFromPool()
    {
        if (_available.Count == 0)
            return null;
        return _available.Dequeue();
    }

    private void ReturnToPool(LeafBehaviour leaf)
    {
        leaf.gameObject.SetActive(false);
        _available.Enqueue(leaf);
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    private void SpawnOne()
    {
        LeafBehaviour leaf = GetFromPool();
        if (leaf == null)
            return; // pool épuisé

        // Position aléatoire dans la zone de spawn
        Vector3 offset = new Vector3(
            Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
            spawnHeight,
            Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f)
        );

        leaf.transform.SetPositionAndRotation(transform.position + offset, Random.rotation);
        leaf.gameObject.SetActive(true);

        // S'abonner à l'événement de pose pour recycler la feuille
        leaf.OnRested += HandleLeafRested;

        leaf.Init();
    }

    // ── Recyclage ─────────────────────────────────────────────────────────────

    private void HandleLeafRested(LeafBehaviour leaf)
    {
        leaf.OnRested -= HandleLeafRested;
        if (!keepRestedLeaves)
            ReturnToPool(leaf);
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        Vector3 center = transform.position + Vector3.up * spawnHeight;
        Gizmos.DrawCube(center, new Vector3(spawnAreaSize.x, 0.05f, spawnAreaSize.y));
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(center, new Vector3(spawnAreaSize.x, 0.05f, spawnAreaSize.y));
    }
#endif
}
