using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Clic sur une surface → crée un GameObject avec un BezierShape ProBuilder
/// dont les points suivent la géométrie de la scène.
/// Utilise la reflection (même approche que IvyGrowth) pour rester compatible
/// avec toutes les versions de ProBuilder.
/// </summary>
public class SurfacePathDrawer : MonoBehaviour
{
    [Header("Chemin")]
    public float pathLength = 10f;
    public float stepSize = 0.5f;
    public LayerMask surfaceMask = ~0;
    public float surfaceOffset = 0.03f;
    public float raycastDownDistance = 2f;

    [Header("BezierShape")]
    public float pathRadius = 0.15f; // rayon du tube extrudé
    public int rows = 8; // segments le long de la courbe
    public int columns = 6; // segments autour du tube

    [Header("Virage aléatoire")]
    [Range(0f, 45f)]
    public float maxAngleVariation = 15f;

    [Header("Matériau (optionnel)")]
    public Material pathMaterial;

    private Camera _cam;

    // ─── Types ProBuilder récupérés par reflection ────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────

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
            Debug.LogError("[SurfacePathDrawer] BezierShape introuvable — ProBuilder installé ?");
            return;
        }

        // Champ "points" (public dans ProBuilder)
        _pointsField = _bezierShapeType.GetField(
            "points",
            BindingFlags.Public | BindingFlags.Instance
        );
        _radiusField = _bezierShapeType.GetField(
            "radius",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );
        _rowsField = _bezierShapeType.GetField(
            "rows",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );
        _columnsField = _bezierShapeType.GetField(
            "columns",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );
        _smoothField = _bezierShapeType.GetField(
            "smooth",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );
        _closeLoopField = _bezierShapeType.GetField(
            "closeLoop",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );
        _refreshMethod = _bezierShapeType.GetMethod(
            "Refresh",
            BindingFlags.Public | BindingFlags.Instance
        );

        // Trouver le type BezierPoint
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            _bezierPointType = asm.GetType("UnityEngine.ProBuilder.BezierPoint");
            if (_bezierPointType != null)
                break;
        }

        if (_pointsField == null)
            Debug.LogError("[SurfacePathDrawer] Champ 'points' introuvable sur BezierShape !");
        if (_bezierPointType == null)
            Debug.LogError("[SurfacePathDrawer] BezierPoint introuvable !");
        if (_refreshMethod == null)
            Debug.LogError("[SurfacePathDrawer] Méthode 'Refresh' introuvable sur BezierShape !");
    }

    // ─────────────────────────────────────────────────────────────────────────

    void TryDrawPath()
    {
        if (_bezierShapeType == null || _bezierPointType == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _cam.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, surfaceMask))
        {
            Debug.LogWarning("[SurfacePathDrawer] Aucune surface touchée.");
            if (Physics.Raycast(ray, out RaycastHit any, Mathf.Infinity))
                Debug.Log(
                    $"[SurfacePathDrawer] Sans mask → '{any.collider.name}' layer {any.collider.gameObject.layer}"
                );
            return;
        }

        List<Vector3> pts = BuildControlPoints(
            hit.point + hit.normal * surfaceOffset,
            hit.normal,
            GetRandomDirectionOnSurface(hit.normal)
        );

        if (pts.Count < 2)
        {
            Debug.LogWarning("[SurfacePathDrawer] Pas assez de points.");
            return;
        }

        CreateBezierShape(pts);
    }

    // ─────────────────────────────────────────────────────────────────────────

    List<Vector3> BuildControlPoints(Vector3 start, Vector3 startNormal, Vector3 startDir)
    {
        var points = new List<Vector3> { start };
        Vector3 pos = start;
        Vector3 normal = startNormal;
        Vector3 dir = startDir;
        float dist = 0f;
        int max = Mathf.CeilToInt(pathLength / stepSize) + 5;

        for (int i = 0; i < max && dist < pathLength; i++)
        {
            dir =
                Quaternion.AngleAxis(Random.Range(-maxAngleVariation, maxAngleVariation), normal)
                * dir;
            dir = Vector3.ProjectOnPlane(dir, normal).normalized;
            if (dir.sqrMagnitude < 0.001f)
                dir = GetRandomDirectionOnSurface(normal);

            Vector3 candidate = pos + dir * stepSize + normal * raycastDownDistance;

            if (
                Physics.Raycast(
                    candidate,
                    -normal,
                    out RaycastHit h,
                    raycastDownDistance * 2f,
                    surfaceMask
                )
            )
            {
                pos = h.point + h.normal * surfaceOffset;
                normal = h.normal;
                dir = Vector3.ProjectOnPlane(dir, normal).normalized;
                points.Add(pos);
                dist += stepSize;
            }
            else
                break;
        }
        return points;
    }

    Vector3 GetRandomDirectionOnSurface(Vector3 normal)
    {
        Vector3 r = Random.onUnitSphere;
        Vector3 t = Vector3.Cross(normal, r).normalized;
        if (t.sqrMagnitude < 0.001f)
            t = Vector3.Cross(normal, r + Vector3.up).normalized;
        return t;
    }

    // ─────────────────────────────────────────────────────────────────────────

    void CreateBezierShape(List<Vector3> pts)
    {
        GameObject go = new GameObject("BezierPath");

        // Ajoute BezierShape via reflection
        var bezierShape = go.AddComponent(_bezierShapeType);

        // Construit la liste de BezierPoint
        var listType = typeof(List<>).MakeGenericType(_bezierPointType);
        var bpList = System.Activator.CreateInstance(listType) as IList;

        // Récupère les champs de BezierPoint
        var posField = _bezierPointType.GetField(
            "position",
            BindingFlags.Public | BindingFlags.Instance
        );
        var tanInField = _bezierPointType.GetField(
            "tangentIn",
            BindingFlags.Public | BindingFlags.Instance
        );
        var tanOutField = _bezierPointType.GetField(
            "tangentOut",
            BindingFlags.Public | BindingFlags.Instance
        );
        var rotField = _bezierPointType.GetField(
            "rotation",
            BindingFlags.Public | BindingFlags.Instance
        );

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 tangentDir;
            if (i < pts.Count - 1)
                tangentDir = (pts[i + 1] - pts[i]).normalized * stepSize * 0.4f;
            else
                tangentDir = (pts[i] - pts[i - 1]).normalized * stepSize * 0.4f;

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

        Debug.Log($"[SurfacePathDrawer] ✅ BezierShape créé ({pts.Count} points de contrôle)");
    }
}
