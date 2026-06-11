using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionManager
    : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IDragHandler
{
    [SerializeField]
    private GameState gameState;

    [Header("Thresholds")]
    [SerializeField]
    private float holdThreshold = 0.3f;

    [SerializeField]
    private float dragThreshold = 1f;

    private float _pointerDownTime;
    private Coroutine _holdRoutine;

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDownTime = Time.time;
        _holdRoutine = StartCoroutine(HoldRoutine());
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.delta.magnitude > dragThreshold)
        {
            StopHoldRoutine();
            gameState.SetInteraction(GameState.InteractionType.Drag, eventData.position);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopHoldRoutine();

        if (gameState.CurrentInteraction == GameState.InteractionType.Drag)
        {
            gameState.SetInteraction(GameState.InteractionType.None, eventData.position);
            return;
        }

        if (gameState.CurrentInteraction != GameState.InteractionType.Hold)
        {
            gameState.SetInteraction(GameState.InteractionType.Click, eventData.position);
        }

        gameState.SetInteraction(GameState.InteractionType.None, eventData.position);
    }

    private IEnumerator HoldRoutine()
    {
        yield return new WaitForSeconds(holdThreshold);
        gameState.SetInteraction(GameState.InteractionType.Hold);
    }

    private void StopHoldRoutine()
    {
        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }
    }
}
