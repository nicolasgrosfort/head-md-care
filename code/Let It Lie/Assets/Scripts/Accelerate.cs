using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Accelerate : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;
    public Volume globalVolume;

    private Vector3 _originalPosition;
    private LensDistortion _lensDistortion;
    private DepthOfField _depthOfField;
    private ColorAdjustments _colorAdjustments;
    private ChromaticAberration _chromaticAberration;

    void Start()
    {
        _originalPosition = Camera.main.transform.localPosition;

        globalVolume.profile.TryGet(out _lensDistortion);
        globalVolume.profile.TryGet(out _depthOfField);
        globalVolume.profile.TryGet(out _colorAdjustments);
        globalVolume.profile.TryGet(out _chromaticAberration);
    }

    void Update()
    {
        float acceleration = gameState.NormalisedTimeSpeed;

        Camera.main.fieldOfView = Mathf.Lerp(60f, 65f, acceleration);
        Camera.main.transform.localPosition =
            _originalPosition + Random.insideUnitSphere * acceleration * 0.1f;

        _lensDistortion.intensity.value = Mathf.Lerp(0f, -0.3f, acceleration);
        _depthOfField.focusDistance.value = Mathf.Lerp(3f, 2f, acceleration);
        _colorAdjustments.saturation.value = Mathf.Lerp(0f, -50f, acceleration);
        _chromaticAberration.intensity.value = Mathf.Lerp(0f, 0.6f, acceleration);
    }
}
