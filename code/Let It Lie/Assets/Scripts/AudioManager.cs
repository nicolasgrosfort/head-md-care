using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        gameState.OnSpeedChange += HandlePitch;
        gameState.OnLifeChange += HandleVolume;
    }

    void OnDisable()
    {
        gameState.OnSpeedChange -= HandlePitch;
        gameState.OnLifeChange -= HandleVolume;
    }

    private void HandlePitch(float _)
    {
        audioSource.pitch = Mathf.Lerp(1f, 3f, gameState.NormalisedTimeSpeed);
    }

    private void HandleVolume(float _)
    {
        audioSource.volume = Mathf.Lerp(0f, 1f, gameState.life);
    }
}
