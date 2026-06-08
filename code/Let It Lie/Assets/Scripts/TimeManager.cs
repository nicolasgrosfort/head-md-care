using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TimeManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private GameState gameState;

    [SerializeField]
    private float _holdDuration = 0.5f;
    private float _downTime = -1f;
    private bool _holdFired;
    private Coroutine _speedRoutine;

    private void Update()
    {
        // Le temps passe toujours
        gameState.IncreaseTime();

        // Détection du hold
        if (_downTime >= 0 && !_holdFired && Time.time - _downTime >= _holdDuration)
        {
            _holdFired = true;
            _speedRoutine = StartCoroutine(AccelerateRoutine());
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        _downTime = Time.time;
        _holdFired = false;
    }

    public void OnPointerUp(PointerEventData e)
    {
        _downTime = -1f;

        if (_speedRoutine != null)
        {
            StopCoroutine(_speedRoutine);
            _speedRoutine = StartCoroutine(DecelerateRoutine());
        }
    }

    // Monte la vitesse tant qu'on hold
    private IEnumerator AccelerateRoutine()
    {
        while (true)
        {
            gameState.IncreaseTimeSpeed();
            yield return null;
        }
    }

    // Redescend la vitesse au relâché
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
