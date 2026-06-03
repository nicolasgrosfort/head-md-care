using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SkyColor : MonoBehaviour
{
    [Header("Volume Post-Process")]
    public Volume volume;

    [Header("Teinte (Hue Shift)")]
    public float hueAtFull = 0f; // teinte à 100% de mousse
    public float hueAtEmpty = 180f; // teinte à 0% de mousse

    [Header("Saturation")]
    public float saturationAtFull = 20f; // +20 à 100%
    public float saturationAtEmpty = -50f; // désaturé à 0%

    [Header("Luminosité")]
    public float brightnessAtFull = 0f;
    public float brightnessAtEmpty = -0.3f;

    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private ChromaticAberration _chromaticAberration;

    void Start()
    {
        if (
            volume != null
            && volume.profile.TryGet(out _colorAdjustments)
            && volume.profile.TryGet(out _vignette)
            && volume.profile.TryGet(out _chromaticAberration)
        )
            return;

        Debug.LogWarning("SkyColor: Color Adjustments introuvable sur le Volume.");
    }

    void Update()
    {
        if (MossCounter.Instance == null || _colorAdjustments == null)
            return;

        float t = MossCounter.Instance.Percentage / 100f;

        _colorAdjustments.saturation.value = Mathf.Lerp(saturationAtEmpty, saturationAtFull, t);
        _vignette.intensity.value = Mathf.Lerp(0.5f, 0.1f, t);
        _chromaticAberration.intensity.value = Mathf.Lerp(0.5f, 0.1f, t);
    }
}
