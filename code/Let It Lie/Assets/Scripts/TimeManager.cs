using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TimeManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;

    private Coroutine _speedRoutine;
    private Coroutine _timeRoutine;

    private void OnEnable()
    {
        gameState.OnHold += StartAccelerate;
        gameState.OnInteractionEnd += StartDecelerate;
        gameState.OnSpeedChange += UpdateTimeScale;

        _timeRoutine = StartCoroutine(TimeRoutine());
    }

    private void OnDisable()
    {
        gameState.OnHold -= StartAccelerate;
        gameState.OnInteractionEnd -= StartDecelerate;
        gameState.OnSpeedChange -= UpdateTimeScale;

        if (_timeRoutine != null)
            StopCoroutine(_timeRoutine);

        Time.timeScale = 1f;
    }

    private void StartAccelerate(PointerEventData eventData)
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

    private void UpdateTimeScale(float speed)
    {
        float normalized = Mathf.InverseLerp(
            gameState.defaultTimeSpeed,
            gameState.maxTimeSpeed,
            speed
        );

        Time.timeScale = Mathf.Lerp(1f, gameState.timeScale, normalized);
    }

    private IEnumerator AccelerateRoutine()
    {
        while (true)
        {
            gameState.IncreaseTimeSpeed(Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private IEnumerator DecelerateRoutine()
    {
        while (gameState.timeSpeed > gameState.defaultTimeSpeed)
        {
            gameState.DecreaseTimeSpeed(Time.unscaledDeltaTime);
            yield return null;
        }

        gameState.SetTimeSpeed(gameState.defaultTimeSpeed);
        Time.timeScale = 1f;
        _speedRoutine = null;
    }

    private IEnumerator TimeRoutine()
    {
        while (true)
        {
            gameState.IncreaseTime(Time.unscaledDeltaTime);
            yield return null;
        }
    }
}
