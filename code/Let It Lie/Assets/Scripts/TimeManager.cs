using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;

    private Coroutine _speedRoutine;

    private void OnEnable()
    {
        gameState.OnHold += StartAccelerate;
        gameState.OnInteractionEnd += StartDecelerate;
        gameState.OnSpeedChange += UpdateTimeScale;
    }

    private void OnDisable()
    {
        gameState.OnHold -= StartAccelerate;
        gameState.OnInteractionEnd -= StartDecelerate;
        gameState.OnSpeedChange -= UpdateTimeScale;
    }

    private void Update()
    {
        gameState.IncreaseTime();
    }

    private void StartAccelerate()
    {
        if (_speedRoutine != null)
            StopCoroutine(_speedRoutine);
        _speedRoutine = StartCoroutine(AccelerateRoutine());
    }

    private void StartDecelerate()
    {
        if (_speedRoutine != null)
            StopCoroutine(_speedRoutine);
        _speedRoutine = StartCoroutine(DecelerateRoutine());
    }

    private IEnumerator AccelerateRoutine()
    {
        while (true)
        {
            gameState.IncreaseTimeSpeed();
            yield return null;
        }
    }

    private IEnumerator DecelerateRoutine()
    {
        while (gameState.timeSpeed > gameState.defaultTimeSpeed)
        {
            gameState.DecreaseTimeSpeed();
            yield return null;
        }
        _speedRoutine = null;
    }

    private void UpdateTimeScale(float speed)
    {
        float normalized = Mathf.InverseLerp(gameState.minTimeSpeed, gameState.maxTimeSpeed, speed);
        Time.timeScale = Mathf.Lerp(1f, gameState.timeScale, normalized);
    }
}
