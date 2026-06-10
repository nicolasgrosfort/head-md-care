using UnityEngine;
using UnityEngine.EventSystems;

public class HandleLeafClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Bombe")]
    public float bombRadius = 10f;
    public GameState gameState;

    public void OnPointerClick(PointerEventData eventData)
    {
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            LeafBehaviour leaf = hit.collider.GetComponent<LeafBehaviour>();
            if (leaf != null)
            {
                if (leaf.IsLanded())
                {
                    leaf.CatchLeaf();
                    return;
                }
                else
                {
                    ExplodeSlowBomb(hit.point);
                    return;
                }
            }
        }
    }

    private void ExplodeSlowBomb(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, bombRadius);
        foreach (Collider col in hits)
        {
            LeafBehaviour leaf = col.GetComponent<LeafBehaviour>();
            if (leaf == null)
                continue;

            float dist = Vector3.Distance(center, col.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / bombRadius); // 1 au centre, 0 au bord
            leaf.ApplySlowBomb(falloff);
        }
    }
}
