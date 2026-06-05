using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class SurfacePathDrawer : MonoBehaviour
{
    [Header("Chemin")]
    public float pathLength = 10f;
    public LayerMask surfaceMask = ~0;
    public float surfaceOffset = 0.03f;

    [Tooltip(
        "Distance du raycast vers le bas pour coller à la surface.\nAugmente si le chemin s'arrête trop tôt."
    )]
    public float raycastDownDistance = 1f;

    [Tooltip(
        "Rayon de recherche quand la surface est perdue.\nPermet de passer les arêtes et les creux."
    )]
    public float lostSurfaceSearchRadius = 0.3f;

    [Header("Espacement des points")]
    [Tooltip("Distance entre chaque point de contrôle Bézier (mètres)")]
    public float pointSpacing = 0.5f;

    [Header("BezierShape")]
    public float pathRadius = 0.15f;
    public int rows = 8;
    public int columns = 6;

    [Header("Virage aléatoire")]
    [Range(0f, 45f)]
    public float maxAngleVariation = 15f;

    [Header("Matériau (optionnel)")]
    public Material pathMaterial;

    private Camera _cam;

    private System.Type _bezierShapeType;
    private System.Type _bezierPointType;
    private FieldInfo _pointsField;
    private FieldInfo _radiusField;
    private FieldInfo _rowsField;
    private FieldInfo _columnsField;
    private FieldInfo _smoothField;
    private FieldInfo _closeLoopField;
    private MethodInfo _refreshMethod;

    void Awake()
    {
        _cam = Camera.main;
        CacheReflection();
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryDrawPath();
    }

    void CacheReflection()
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            _bezierShapeType = asm.GetType("UnityEngine.ProBuilder.BezierShape");
            if (_bezierShapeType != null)
                break;
        }
        if (_bezierShapeType == null)
        {
            Debug.LogError("[SurfacePathDrawer] BezierShape introuvable !");
            return;
        }

        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            _bezierPointType = asm.GetType("UnityEngine.ProBuilder.BezierPoint");
            if (_bezierPointType != null)
                break;
        }

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        _pointsField = _bezierShapeType.GetField(
            "points",
            BindingFlags.Public | BindingFlags.Instance
        );
        _radiusField = _bezierShapeType.GetField("radius", flags);
        _rowsField = _bezierShapeType.GetField("rows", flags);
        _columnsField = _bezierShapeType.GetField("columns", flags);
        _smoothField = _bezierShapeType.GetField("smooth", flags);
        _closeLoopField = _bezierShapeType.GetField("closeLoop", flags);
        _refreshMethod = _bezierShapeType.GetMethod(
            "Refresh",
            BindingFlags.Public | BindingFlags.Instance
        );
    }

    void TryDrawPath()
    {
        if (_bezierShapeType == null || _bezierPointType == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _cam.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, surfaceMask))
            return; // clic dans le vide, silencieux

        float internalStep = Mathf.Min(pointSpacing * 0.25f, 0.05f);

        List<Vector3> densePoints = BuildDensePoints(
            hit.point + hit.normal * surfaceOffset,
            hit.normal,
            GetRandomDirectionOnSurface(hit.normal),
            internalStep
        );

        if (densePoints.Count < 2)
            return;

        List<Vector3> spacedPoints = ResampleByDistance(densePoints, pointSpacing);
        if (spacedPoints.Count < 2)
            return;

        Debug.Log(
            $"[SurfacePathDrawer] {densePoints.Count} pts denses → {spacedPoints.Count} pts espacés"
        );
        CreateBezierShape(spacedPoints);
    }

    List<Vector3> BuildDensePoints(Vector3 start, Vector3 startNormal, Vector3 startDir, float step)
    {
        var points = new List<Vector3> { start };
        Vector3 pos = start;
        Vector3 normal = startNormal;
        Vector3 dir = startDir;
        float dist = 0f;
        int max = Mathf.CeilToInt(pathLength / step) + 10;
        int lostStreak = 0;

        for (int i = 0; i < max && dist < pathLength; i++)
        {
            dir =
                Quaternion.AngleAxis(
                    Random.Range(-maxAngleVariation, maxAngleVariation) * step,
                    normal
                ) * dir;
            dir = Vector3.ProjectOnPlane(dir, normal).normalized;
            if (dir.sqrMagnitude < 0.001f)
                dir = GetRandomDirectionOnSurface(normal);

            Vector3 candidate = pos + dir * step + normal * raycastDownDistance;
            bool found = false;

            // ── Tentative 1 : raycast direct vers le bas ──────────────────────
            if (
                Physics.Raycast(
                    candidate,
                    -normal,
                    out RaycastHit h,
                    raycastDownDistance * 2.5f,
                    surfaceMask
                )
            )
            {
                pos = h.point + h.normal * surfaceOffset;
                normal = h.normal;
                dir = Vector3.ProjectOnPlane(dir, normal).normalized;
                points.Add(pos);
                dist += step;
                found = true;
                lostStreak = 0;
            }
            else
            {
                // ── Tentative 2 : recherche sphérique autour du candidat ──────
                Collider[] nearby = Physics.OverlapSphere(
                    candidate,
                    lostSurfaceSearchRadius,
                    surfaceMask
                );
                if (nearby.Length > 0)
                {
                    // Prend le point le plus proche sur le collider le plus near
                    Vector3 closest = nearby[0].ClosestPoint(candidate);
                    Vector3 approxNormal = (candidate - closest).normalized;

                    // Raycast depuis au-dessus de ce point trouvé
                    Vector3 above = closest + approxNormal * raycastDownDistance;
                    if (
                        Physics.Raycast(
                            above,
                            -approxNormal,
                            out RaycastHit h2,
                            raycastDownDistance * 2f,
                            surfaceMask
                        )
                    )
                    {
                        pos = h2.point + h2.normal * surfaceOffset;
                        normal = h2.normal;
                        dir = Vector3.ProjectOnPlane(dir, normal).normalized;
                        points.Add(pos);
                        dist += step;
                        found = true;
                        lostStreak = 0;
                    }
                }
            }

            if (!found)
            {
                lostStreak++;
                // Tolère 3 steps perdus consécutifs avant d'arrêter
                if (lostStreak >= 3)
                    break;
            }
        }
        return points;
    }

    List<Vector3> ResampleByDistance(List<Vector3> input, float spacing)
    {
        var result = new List<Vector3> { input[0] };
        float accumulated = 0f;

        for (int i = 1; i < input.Count; i++)
        {
            float segLen = Vector3.Distance(input[i - 1], input[i]);
            if (segLen < 0.0001f)
                continue;
            accumulated += segLen;

            while (accumulated >= spacing)
            {
                float overflow = accumulated - spacing;
                float t = 1f - (overflow / segLen);
                result.Add(Vector3.Lerp(input[i - 1], input[i], t));
                accumulated -= spacing;
            }
        }

        if (Vector3.Distance(result[result.Count - 1], input[input.Count - 1]) > 0.01f)
            result.Add(input[input.Count - 1]);

        return result;
    }

    Vector3 GetRandomDirectionOnSurface(Vector3 normal)
    {
        Vector3 r = Random.onUnitSphere;
        Vector3 t = Vector3.Cross(normal, r).normalized;
        if (t.sqrMagnitude < 0.001f)
            t = Vector3.Cross(normal, r + Vector3.up).normalized;
        return t;
    }

    void CreateBezierShape(List<Vector3> pts)
    {
        GameObject go = new GameObject("BezierPath");
        var bezierShape = go.AddComponent(_bezierShapeType);

        var listType = typeof(List<>).MakeGenericType(_bezierPointType);
        var bpList = System.Activator.CreateInstance(listType) as IList;

        var flags = BindingFlags.Public | BindingFlags.Instance;
        var posField = _bezierPointType.GetField("position", flags);
        var tanInField = _bezierPointType.GetField("tangentIn", flags);
        var tanOutField = _bezierPointType.GetField("tangentOut", flags);
        var rotField = _bezierPointType.GetField("rotation", flags);

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 tangentDir;
            if (i == 0)
                tangentDir = (pts[1] - pts[0]).normalized * pointSpacing * 0.4f;
            else if (i == pts.Count - 1)
                tangentDir = (pts[i] - pts[i - 1]).normalized * pointSpacing * 0.4f;
            else
                tangentDir = (pts[i + 1] - pts[i - 1]).normalized * pointSpacing * 0.4f;

            var bp = System.Activator.CreateInstance(_bezierPointType);
            posField?.SetValue(bp, pts[i]);
            tanInField?.SetValue(bp, pts[i] - tangentDir);
            tanOutField?.SetValue(bp, pts[i] + tangentDir);
            rotField?.SetValue(bp, Quaternion.identity);
            bpList.Add(bp);
        }

        _pointsField?.SetValue(bezierShape, bpList);
        _radiusField?.SetValue(bezierShape, pathRadius);
        _rowsField?.SetValue(bezierShape, rows);
        _columnsField?.SetValue(bezierShape, columns);
        _smoothField?.SetValue(bezierShape, true);
        _closeLoopField?.SetValue(bezierShape, false);
        _refreshMethod?.Invoke(bezierShape, null);

        if (pathMaterial != null)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.material = pathMaterial;
        }

        Debug.Log($"[SurfacePathDrawer] ✅ BezierShape créé ({pts.Count} points)");
    }
}
