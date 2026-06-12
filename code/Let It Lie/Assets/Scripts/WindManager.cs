using System.Collections;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField]
    private GameState gameState;

    [Header("Position du souffle")]
    [SerializeField]
    private Transform pointDeSouffle;

    public LayerMask layerMask = ~0;

    public float force = 10f;
    public float dureeImpulsion = 0.5f;
    public float porteeMax = 200f;
    public float angleCone = 20f;

    void OnEnable() => gameState.OnClick += OnClick;

    void OnDisable() => gameState.OnClick -= OnClick;

    void OnClick(Vector2 screenPos)
    {
        StartCoroutine(Blow(screenPos));
    }

    IEnumerator Blow(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        Debug.DrawRay(ray.origin, ray.direction * porteeMax, Color.red, 5f);

        if (Physics.Raycast(ray, out RaycastHit hit, porteeMax, layerMask))
        {
            Debug.Log("Hit " + hit.collider.name);
        }

        yield return null;
    }
}
