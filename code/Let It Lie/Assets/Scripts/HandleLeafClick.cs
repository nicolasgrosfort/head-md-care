using UnityEngine;
using UnityEngine.EventSystems;

public class HandleLeafClick : MonoBehaviour, IPointerClickHandler
{
    public GameState gameState;

    public void OnPointerClick(PointerEventData eventData)
    {
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            LeafBehaviour leaf = hit.collider.GetComponent<LeafBehaviour>();
            if (leaf != null)
            {
                leaf.OnPointerClick(eventData);
                return;
            }
        }
    }
}
