using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SurfacePathDrawer : MonoBehaviour
{
    [Header("Caméra")]
    public Camera cam;

    [Header("Branche")]
    public int maxPoints = 40;
    public float segmentLength = 0.02f;
    public float branchRadius = 0.02f;

    [Range(0f, 45f)]
    public float maxAngle = 20f;
    public int meshFaces = 8;

    [Tooltip("Combien de steps sans surface avant d'abandonner")]
    public int maxLostStreak = 8;

    [Tooltip("Distance max de recherche quand la surface est perdue")]
    public float searchRadius = 0.1f;

    [Header("Matériau")]
    public Material branchMaterial;

    int _count = 0;

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                SpawnBranch(hit);
        }
    }

    void SpawnBranch(RaycastHit hit)
    {
        Vector3 tangent = FindTangent(hit.normal);
        Vector3 dir = Quaternion.AngleAxis(Random.Range(0f, 360f), hit.normal) * tangent;

        List<PathNode> nodes = CreateNodes(hit.point, hit.normal, dir);
        if (nodes == null || nodes.Count < 2)
        {
            Debug.LogWarning("[SurfacePathDrawer] Pas assez de noeuds");
            return;
        }

        GameObject root = new GameObject("Branch_" + _count++);
        root.transform.SetParent(transform);

        MeshFilter mf = root.AddComponent<MeshFilter>();
        MeshRenderer mr = root.AddComponent<MeshRenderer>();
        mf.mesh = BuildMesh(nodes);
        mr.material =
            branchMaterial != null
                ? branchMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));

        Debug.Log($"[SurfacePathDrawer] Branche : {nodes.Count} noeuds");
    }

    // ─────────────────────────────────────────────────────────────────────────

    class PathNode
    {
        public Vector3 position;
        public Vector3 normal;

        public PathNode(Vector3 p, Vector3 n)
        {
            position = p;
            normal = n;
        }
    }

    // ── Génération itérative (plus robuste que récursive) ─────────────────────

    List<PathNode> CreateNodes(Vector3 startPos, Vector3 startNormal, Vector3 startDir)
    {
        var nodes = new List<PathNode>();
        nodes.Add(new PathNode(startPos, startNormal));

        Vector3 pos = startPos;
        Vector3 normal = startNormal;
        Vector3 dir = startDir;
        int lost = 0;

        for (int i = 1; i < maxPoints; i++)
        {
            // Variation angulaire tous les 2 steps
            if (i % 2 == 0)
                dir = Quaternion.AngleAxis(Random.Range(-maxAngle, maxAngle), normal) * dir;
            dir = Vector3.ProjectOnPlane(dir, normal).normalized;

            Vector3 nextPos = pos;
            Vector3 nextNormal = normal;
            bool found = false;

            // ── Avance le long de la normale (reste collé) ────────────────────
            Vector3 p1 = pos + normal * segmentLength;
            if (Physics.Raycast(new Ray(pos, normal), out RaycastHit h0, segmentLength))
                p1 = h0.point;

            // ── Cas 1 : mur devant ────────────────────────────────────────────
            if (Physics.Raycast(new Ray(p1, dir), out RaycastHit h1, segmentLength))
            {
                nextPos = h1.point;
                nextNormal = -dir;
                dir = normal; // tourne autour du coin
                found = true;
            }
            else
            {
                Vector3 p2 = p1 + dir * segmentLength;

                // ── Cas 2 : surface plate ─────────────────────────────────────
                if (
                    Physics.Raycast(
                        new Ray(p2 + normal * 0.01f, -normal),
                        out RaycastHit h2,
                        segmentLength * 2f
                    )
                )
                {
                    nextPos = h2.point;
                    nextNormal = h2.normal;
                    found = true;
                }
                // ── Cas 3 : pente descendante ─────────────────────────────────
                else if (
                    Physics.Raycast(
                        new Ray(p2 - normal * segmentLength + normal * 0.01f, -normal),
                        out RaycastHit h3,
                        segmentLength * 2f
                    )
                )
                {
                    nextPos = h3.point;
                    nextNormal = h3.normal;
                    found = true;
                }
                // ── Cas 4 : recherche sphérique (arête, creux, gap) ───────────
                else
                {
                    Vector3 searchOrigin = p2;
                    Collider[] nearby = Physics.OverlapSphere(searchOrigin, searchRadius);
                    float bestDist = float.MaxValue;
                    Vector3 bestPos = Vector3.zero;
                    Vector3 bestNormal = normal;

                    foreach (var col in nearby)
                    {
                        Vector3 closest = col.ClosestPoint(searchOrigin);
                        float d = Vector3.Distance(searchOrigin, closest);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestPos = closest;
                            // Raycast pour récupérer la vraie normale
                            Vector3 fromAbove = closest + normal * 0.05f;
                            if (Physics.Raycast(fromAbove, -normal, out RaycastHit hN, 0.2f))
                            {
                                bestPos = hN.point;
                                bestNormal = hN.normal;
                            }
                        }
                    }

                    if (bestDist < float.MaxValue)
                    {
                        nextPos = bestPos;
                        nextNormal = bestNormal;
                        found = true;
                    }
                }
            }

            if (found)
            {
                // Offset de surface pour éviter le z-fighting
                nextPos += nextNormal * 0.005f;
                nodes.Add(new PathNode(nextPos, nextNormal));
                pos = nextPos;
                normal = nextNormal;
                dir = Vector3.ProjectOnPlane(dir, normal).normalized;
                if (dir.sqrMagnitude < 0.001f)
                    dir = FindTangent(normal);
                lost = 0;
            }
            else
            {
                lost++;
                // Continue d'avancer "dans le vide" en gardant la direction
                // pour avoir une chance de retomber sur une surface
                pos += dir * segmentLength;
                if (lost >= maxLostStreak)
                {
                    Debug.LogWarning(
                        $"[SurfacePathDrawer] Abandon après {nodes.Count} noeuds (surface introuvable)"
                    );
                    break;
                }
            }
        }

        return nodes;
    }

    // ── Mesh tubulaire ────────────────────────────────────────────────────────

    Mesh BuildMesh(List<PathNode> nodes)
    {
        Mesh mesh = new Mesh();
        int n = nodes.Count;

        Vector3[] vertices = new Vector3[n * meshFaces];
        Vector3[] normals = new Vector3[n * meshFaces];
        Vector2[] uv = new Vector2[n * meshFaces];
        int[] triangles = new int[(n - 1) * meshFaces * 6];

        float vStep = (2f * Mathf.PI) / meshFaces;

        for (int i = 0; i < n; i++)
        {
            Vector3 fw = Vector3.zero;
            if (i > 0)
                fw += nodes[i - 1].position - nodes[i].position;
            if (i < n - 1)
                fw += nodes[i].position - nodes[i + 1].position;
            if (fw == Vector3.zero)
                fw = Vector3.forward;
            fw.Normalize();

            Vector3 up = nodes[i].normal.normalized;
            Quaternion orientation = Quaternion.LookRotation(fw, up);

            for (int v = 0; v < meshFaces; v++)
            {
                Vector3 p =
                    nodes[i].position
                    + orientation * Vector3.up * (branchRadius * Mathf.Sin(v * vStep))
                    + orientation * Vector3.right * (branchRadius * Mathf.Cos(v * vStep));

                vertices[i * meshFaces + v] = p;
                normals[i * meshFaces + v] = (p - nodes[i].position).normalized;
                uv[i * meshFaces + v] = new Vector2((float)v / meshFaces, (float)i / (n - 1));
            }

            if (i < n - 1)
            {
                for (int v = 0; v < meshFaces; v++)
                {
                    int t = i * meshFaces * 6 + v * 6;
                    triangles[t] = ((v + 1) % meshFaces) + i * meshFaces;
                    triangles[t + 1] = triangles[t + 4] = v + i * meshFaces;
                    triangles[t + 2] = triangles[t + 3] =
                        ((v + 1) % meshFaces + meshFaces) + i * meshFaces;
                    triangles[t + 5] = (meshFaces + v % meshFaces) + i * meshFaces;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.RecalculateBounds();
        return mesh;
    }

    Vector3 FindTangent(Vector3 normal)
    {
        Vector3 t1 = Vector3.Cross(normal, Vector3.forward);
        Vector3 t2 = Vector3.Cross(normal, Vector3.up);
        return (t1.magnitude > t2.magnitude) ? t1 : t2;
    }
}
