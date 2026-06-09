using UnityEngine;
using UnityEngine.EventSystems;

public class ClicFeedback : MonoBehaviour, IPointerClickHandler
{
    public GameState gameState;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Clicked! Current life: " + gameState.life);
    }
}
