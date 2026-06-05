using System.Collections;
using UnityEngine;

public class LeafSpawner : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject leafPrefab;
    public int totalLeaves = 1000;
    public int leavesPerBatch = 20; // spawn 20 par frame
    public float batchInterval = 0.05f; // toutes les 50ms
    public Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);
    public float spawnHeight = 8f;

    private LeafPool _pool;

    void Start()
    {
        _pool = new LeafPool(leafPrefab, totalLeaves);
        StartCoroutine(SpawnLeaves());
    }

    IEnumerator SpawnLeaves()
    {
        int spawned = 0;
        while (spawned < totalLeaves)
        {
            int batch = Mathf.Min(leavesPerBatch, totalLeaves - spawned);
            for (int i = 0; i < batch; i++)
            {
                Vector3 pos =
                    transform.position
                    + new Vector3(
                        Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                        spawnHeight,
                        Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                    );
                _pool.Spawn(pos, Random.rotation);
            }
            spawned += batch;
            yield return new WaitForSeconds(batchInterval);
        }
    }
}
