using UnityEngine;

public class BreathingSphere : MonoBehaviour
{
    public float breathSpeed = 0.8f;
    public float breathAmplitude = 0.08f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private float[] noiseOffsets;
    private int[] triangles;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        mesh = Instantiate(mesh); // copie unique pour cet objet
        GetComponent<MeshFilter>().mesh = mesh;

        originalVertices = mesh.vertices;
        triangles = mesh.triangles;

        noiseOffsets = new float[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];
            noiseOffsets[i] =
                v.x * 1.7f + v.y * 3.1f + v.z * 5.3f + gameObject.GetHashCode() * 0.0001f;
            ;
        }
    }

    void Update()
    {
        Vector3[] newVertices = new Vector3[originalVertices.Length];

        for (int i = 0; i < newVertices.Length; i++)
        {
            Vector3 orig = originalVertices[i];
            Vector3 dir = orig.normalized;

            float noise = Mathf.PerlinNoise(
                noiseOffsets[i] + Time.time * breathSpeed,
                noiseOffsets[i]
            );

            float displacement = (noise * 2f - 1f) * breathAmplitude;
            newVertices[i] = orig + dir * displacement;
        }

        mesh.vertices = newVertices;
        RecalculateFlatNormals(newVertices); // flat shading maintenu à chaque frame
    }

    void RecalculateFlatNormals(Vector3[] verts)
    {
        Vector3[] flatNormals = new Vector3[verts.Length];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = verts[triangles[i]];
            Vector3 v1 = verts[triangles[i + 1]];
            Vector3 v2 = verts[triangles[i + 2]];
            Vector3 flat = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            flatNormals[triangles[i]] = flat;
            flatNormals[triangles[i + 1]] = flat;
            flatNormals[triangles[i + 2]] = flat;
        }

        mesh.normals = flatNormals;
    }
}
