using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
[RequireComponent(typeof(GameState))]
public class LifeManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;
    public Volume globalVolume;

    private Vignette _vignette;
    private FilmGrain _filmGrain;
    private WhiteBalance _whiteBalance;

    void Start()
    {
        globalVolume.profile.TryGet(out _vignette);
        globalVolume.profile.TryGet(out _filmGrain);
        globalVolume.profile.TryGet(out _whiteBalance);
    }

    void Update()
    {
        _vignette.intensity.value = Mathf.Lerp(0.5f, 0f, gameState.life);
        _filmGrain.intensity.value = Mathf.Lerp(1f, 0f, gameState.life);
        _whiteBalance.temperature.value = Mathf.Lerp(100f, 0f, gameState.life);
        _whiteBalance.tint.value = Mathf.Lerp(100f, 0f, gameState.life);
    }
}
