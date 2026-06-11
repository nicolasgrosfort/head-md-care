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
    }

    private void OnDisable()
    {
        gameState.OnHold -= StartAccelerate;
        gameState.OnInteractionEnd -= StartDecelerate;
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
}
