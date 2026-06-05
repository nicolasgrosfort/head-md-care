using System.Collections.Generic;
using UnityEngine;

public class LeafPool
{
    private Queue<GameObject> _available = new Queue<GameObject>();

    public LeafPool(GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = Object.Instantiate(prefab);
            go.SetActive(false);
            _available.Enqueue(go);
        }
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        if (_available.Count == 0)
            return null;

        var go = _available.Dequeue();
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        go.GetComponent<LeafBehaviour>().Init();
        return go;
    }
}
