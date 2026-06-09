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

        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Radius : départ doux avec SmoothStep
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float ringRadius = Mathf.Lerp(0f, 0.3f, smoothT); // ← max réduit à 0.5

            // Largeur de l'anneau
            float ringWidth = Mathf.Lerp(0.12f, 0.06f, smoothT);

            // Amplitude : montée douce, descente longue
            float amplitude =
                t < 0.15f
                    ? Mathf.Lerp(0f, 0.025f, t / 0.15f) // ← force réduite à 0.025
                    : Mathf.Lerp(0.025f, 0f, (t - 0.15f) / 0.85f);

            // Fréquence : démarre bas, monte doucement
            float frequency = Mathf.Lerp(8f, 18f, smoothT); // ← démarre à 8 au lieu de 20

            rippleMaterial.SetFloat("_Time2", elapsed * 6f);
            rippleMaterial.SetFloat("_Strength", amplitude);
            rippleMaterial.SetFloat("_Radius", ringRadius);
            rippleMaterial.SetFloat("_RingWidth", ringWidth);
            rippleMaterial.SetFloat("_Frequency", frequency);

            yield return null;
        }

        rippleMaterial.SetFloat("_Strength", 0f);
    }
}
