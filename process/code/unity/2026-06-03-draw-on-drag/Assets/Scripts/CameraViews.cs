using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraViews", menuName = "Camera/Views Registry")]
public class CameraViews : ScriptableObject
{
    [System.Serializable]
    public class CameraView
    {
        public string viewName;
        public Vector3 position;
        public Vector3 rotation; // Euler angles
    }

    public List<CameraView> views = new List<CameraView>();

    public CameraView GetView(string name)
    {
        return views.Find(v => v.viewName == name);
    }
}
