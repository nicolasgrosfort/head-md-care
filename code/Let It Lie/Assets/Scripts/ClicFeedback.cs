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
        // Convertit position écran → UV (0-1)
        Vector2 uv = new Vector2(
            eventData.position.x / Screen.width,
            eventData.position.y / Screen.height
        );

        if (_rippleCoroutine != null)
            StopCoroutine(_rippleCoroutine);

        _rippleCoroutine = StartCoroutine(AnimateRipple(uv));
    }

    private IEnumerator AnimateRipple(Vector2 center)
    {
        rippleMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0, 0));

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float waveFront = Mathf.Lerp(0f, 0.15f, smoothT); // ← réduit
            float waveWidth = Mathf.Lerp(0.01f, 0.03f, smoothT);

            float amplitude =
                t < 0.1f
                    ? Mathf.Lerp(0f, 0.006f, t / 0.1f) // ← fix + réduit
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
