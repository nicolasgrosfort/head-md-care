using System.Collections.Generic;
using UnityEngine;

public class TreeManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField]
    private GameState gameState;

    [Header("Leafs")]
    [SerializeField]
    private string leafPrefix = "Leaf.";

    [Header("Flower")]
    [SerializeField]
    private GameObject flowerPrefab;

    private List<Leaf> _leaves = new List<Leaf>();

    private void Awake()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (!child.name.StartsWith(leafPrefix))
                continue;

            if (!child.GetComponent<Rigidbody>())
            {
                Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }

            if (!child.GetComponent<Collider>())
                child.gameObject.AddComponent<BoxCollider>();

            Leaf leaf = child.gameObject.AddComponent<Leaf>();
            leaf.Init(gameState, flowerPrefab);
            _leaves.Add(leaf);
        }
    }
}
