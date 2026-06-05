using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClickToView : MonoBehaviour
{
    [SerializeField]
    CameraViews cameraViews; // glisse le ScriptableObject ici

    [SerializeField]
    string viewName; // nom de la vue à déclencher

    void OnMouseDown()
    {
        CameraController.Instance.GoTo(viewName, cameraViews);
    }
}
