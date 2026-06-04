using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField]
    float duration = 0.8f;

    Coroutine _current;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void GoTo(string viewName, CameraViews registry)
    {
        var view = registry.GetView(viewName);
        if (view == null)
        {
            Debug.LogWarning($"Vue inconnue : {viewName}");
            return;
        }

        if (_current != null)
            StopCoroutine(_current);
        _current = StartCoroutine(Animate(view));
    }

    IEnumerator Animate(CameraViews.CameraView view)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(view.rotation);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = Mathf.SmoothStep(0, 1, t);
            transform.position = Vector3.Lerp(startPos, view.position, e);
            transform.rotation = Quaternion.Slerp(startRot, endRot, e);
            yield return null;
        }

        transform.SetPositionAndRotation(view.position, endRot);
    }
}
