using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class IvyGrowth : MonoBehaviour
{
    [Header("Drag")]
    public float dragDistanceToFull = 200f;
    public Vector2 dragDirection = new Vector2(1f, 0f);

    [Range(0f, 1f)]
    public float growT = 0f;

    private Component bezierShape;
    private List<object> originalPoints = new List<object>();
    private FieldInfo pointsField;
    private MethodInfo refreshMethod;

    private bool isDragging = false;
    private Vector2 dragStart;

    void Start()
    {
        foreach (var comp in GetComponents<Component>())
        {
            if (comp.GetType().Name == "BezierShape")
            {
                bezierShape = comp;
                break;
            }
        }

        if (bezierShape == null)
        {
            Debug.LogError("BezierShape introuvable !");
            return;
        }

        var type = bezierShape.GetType();
        pointsField = type.GetField("points", BindingFlags.Public | BindingFlags.Instance);
        refreshMethod = type.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Instance);

        var list = pointsField.GetValue(bezierShape) as IList;
        foreach (var p in list)
            originalPoints.Add(p);

        Debug.Log($"BezierShape trouvé avec {originalPoints.Count} points.");
    }

    void Update()
    {
        if (bezierShape == null)
            return;
        HandleDrag();
        ApplyGrowth();
    }

    void HandleDrag()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            dragStart = mouse.position.ReadValue();
        }

        if (isDragging && mouse.leftButton.isPressed)
        {
            Vector2 delta = mouse.position.ReadValue() - dragStart;
            float proj = Vector2.Dot(delta, dragDirection.normalized);
            growT = Mathf.Clamp01(proj / dragDistanceToFull);

            Debug.Log($"growT = {growT}"); // ← ajoute cette ligne
        }

        if (mouse.leftButton.wasReleasedThisFrame)
            isDragging = false;
    }

    void ApplyGrowth()
    {
        int total = originalPoints.Count;
        if (total < 2)
            return;

        int count = Mathf.Clamp(Mathf.RoundToInt(growT * total), 2, total);

        var listType = typeof(List<>).MakeGenericType(originalPoints[0].GetType());
        var subList = System.Activator.CreateInstance(listType) as IList;

        for (int i = 0; i < count; i++)
            subList.Add(originalPoints[i]);

        pointsField.SetValue(bezierShape, subList);
        refreshMethod.Invoke(bezierShape, null);
    }
}
