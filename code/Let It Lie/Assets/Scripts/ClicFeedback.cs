using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class ClicFeedback : MonoBehaviour, IPointerClickHandler
{
    public GameState gameState;
    public Material rippleMaterial;
    public float rippleDuration = 0.8f;

    private Coroutine _rippleCoroutine;

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 uv = new Vector2(
            eventData.position.x / Screen.width,
            eventData.position.y / Screen.height
        );

        // Raycast 3D pour récupérer la profondeur
        float depthRadius = 0f; // valeur par défaut si rien touché

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Leaf"))
            {
                // Distance caméra → point touché
                float depth = hit.distance;

                // Plus loin = plus grand, plus proche = plus petit
                // Normalise entre une distance min (1m) et max (20m)
                float t = Mathf.InverseLerp(20f, 120f, depth);
                depthRadius = Mathf.Lerp(0.15f, 0.05f, t);
                Debug.Log($"Hit à {depth:F1}m (t={t:F2})");
            }
        }

        if (_rippleCoroutine != null)
            StopCoroutine(_rippleCoroutine);

        _rippleCoroutine = StartCoroutine(AnimateRipple(uv, 0.1f));
    }

    private IEnumerator AnimateRipple(Vector2 center, float maxRadius)
    {
        rippleMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0, 0));

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float waveFront = Mathf.Lerp(0f, maxRadius, smoothT);
            float waveWidth = Mathf.Lerp(0.01f, maxRadius * 0.2f, smoothT);

            float amplitude =
                t < 0.1f
                    ? Mathf.Lerp(0f, 0.006f, t / 0.1f)
                    : Mathf.Lerp(0.006f, 0f, (t - 0.1f) / 0.9f);

            rippleMaterial.SetFloat("_Time2", elapsed * 10f);
            rippleMaterial.SetFloat("_Strength", amplitude);
            rippleMaterial.SetFloat("_WaveFront", waveFront);
            rippleMaterial.SetFloat("_WaveWidth", waveWidth);

            yield return null;
        }

        rippleMaterial.SetFloat("_Strength", 0f);
    }
}
