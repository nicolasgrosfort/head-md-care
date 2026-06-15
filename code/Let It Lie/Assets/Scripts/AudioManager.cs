using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;

    [Header("Références")]
    [SerializeField]
    private AudioMixer mixer;

    [Header("Reverb")]
    [SerializeField]
    private float dryLevel = 0f;

    [Header("EQ")]
    [SerializeField]
    private float centerFreq = 6500f;

    [SerializeField]
    private float octaveRange = 1.5f;

    [SerializeField]
    private float minGain = 1f;

    private AudioSource audioSource;
    private Coroutine frequencyCoroutine;
    private float _currentT = 0f;
    private bool _bloomActive = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        gameState.OnSpeedChange += HandlePitch;
        gameState.OnLifeChange += HandleVolume;
        gameState.OnBloom += HandleBloom;
    }

    void OnDisable()
    {
        gameState.OnSpeedChange -= HandlePitch;
        gameState.OnLifeChange -= HandleVolume;
        gameState.OnBloom -= HandleBloom;
    }

    private void HandlePitch(float _)
    {
        audioSource.pitch = Mathf.Lerp(1f, 3f, gameState.NormalisedTimeSpeed);
    }

    private void HandleVolume(float _)
    {
        audioSource.volume = Mathf.Lerp(0f, 1f, gameState.life);
    }

    private void HandleBloom(float timeElapsed)
    {
        _bloomActive = true; // appelé uniquement pendant le drag

        if (frequencyCoroutine == null)
            frequencyCoroutine = StartCoroutine(FrequencyCoroutine());
    }

    private IEnumerator FrequencyCoroutine()
    {
        float speed = 1f / 0.6f;

        while (true)
        {
            _bloomActive = false; // reset chaque frame

            yield return null; // attend la prochaine frame

            if (_bloomActive)
                _currentT = Mathf.MoveTowards(_currentT, 1f, speed * Time.deltaTime);
            else
                _currentT = Mathf.MoveTowards(_currentT, 0f, speed * Time.deltaTime);

            mixer.SetFloat("FrequencyGain", Mathf.Lerp(1f, 3f, _currentT));

            if (!_bloomActive && _currentT <= 0f)
            {
                frequencyCoroutine = null;
                yield break;
            }
        }
    }
}
