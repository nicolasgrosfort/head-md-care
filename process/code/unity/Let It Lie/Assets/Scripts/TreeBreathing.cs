using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class TreeBreathing : MonoBehaviour
{
    public float breathSpeed = 0.8f;
    public float breathAmplitude = 0.08f;

    private ProBuilderMesh pbMesh;
    private Vector3[] originalVertices;
    private float[] noiseOffsets;

    // Groupes : chaque groupe = liste d'indices qui partagent la même position
    private List<List<int>> vertexGroups;

    void Start()
    {
        pbMesh = GetComponent<ProBuilderMesh>();

        // Seed unique basé sur l'ID de cet objet
        Random.InitState(gameObject.GetHashCode());

        originalVertices = new Vector3[pbMesh.positions.Count];
        for (int i = 0; i < pbMesh.positions.Count; i++)
            originalVertices[i] = pbMesh.positions[i];

        BuildVertexGroups();
    }

    void BuildVertexGroups()
    {
        vertexGroups = new List<List<int>>();
        bool[] assigned = new bool[originalVertices.Length];
        noiseOffsets = new float[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            if (assigned[i])
                continue;

            var group = new List<int> { i };

            for (int j = i + 1; j < originalVertices.Length; j++)
            {
                if (
                    !assigned[j]
                    && Vector3.Distance(originalVertices[i], originalVertices[j]) < 0.001f
                )
                {
                    group.Add(j);
                    assigned[j] = true;
                }
            }

            // Offset aléatoire partagé par tout le groupe
            float offset = Random.Range(0f, 100f);
            foreach (int idx in group)
                noiseOffsets[idx] = offset;

            assigned[i] = true;
            vertexGroups.Add(group);
        }
    }

    void Update()
    {
        var newPositions = new Vector3[pbMesh.positions.Count];

        for (int i = 0; i < newPositions.Length; i++)
        {
            Vector3 orig = originalVertices[i];
            Vector3 dir = orig.normalized;

            float noise = Mathf.PerlinNoise(
                noiseOffsets[i] + Time.time * breathSpeed,
                noiseOffsets[i]
            );

            float displacement = (noise * 2f - 1f) * breathAmplitude;
            newPositions[i] = orig + dir * displacement;
        }

        pbMesh.positions = newPositions;
        pbMesh.ToMesh();
        pbMesh.Refresh();
    }
}
