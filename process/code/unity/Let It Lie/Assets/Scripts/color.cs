using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class AssignMat : MonoBehaviour
{
    public Material mat;

    [ContextMenu("Apply to Children")]
    void Apply()
    {
        var renderers = GetComponentsInChildren<MeshRenderer>();
        Undo.RecordObjects(renderers, "Assign Material");
        foreach (var r in renderers)
            r.sharedMaterial = mat;
    }
}
#endif