using System.Collections;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField]
    private GameState gameState;

    [Header("Wind")]
    [SerializeField]
    private float windOrigin = 60f;
    public float maxDepth = 200f;
    public LayerMask layerMask = ~0;

    // public float force = 10f;
    // public float dureeImpulsion = 0.5f;
    // public float angleCone = 20f;

    void OnEnable()
    {
        gameState.OnClick += OnClick;
        gameState.OnDrag += OnDrag;
    }

    void OnDisable()
    {
        gameState.OnClick -= OnClick;
        gameState.OnDrag -= OnDrag;
    }

    void OnClick(Vector2 screenPos)
    {
        StartCoroutine(Blow(screenPos));
    }

    void OnDrag(Vector2 screenPos)
    {
        StartCoroutine(Blow(screenPos));
    }

    IEnumerator Blow(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Vector3 shiftedOrigin = ray.origin + ray.direction * windOrigin;

        if (Physics.Raycast(shiftedOrigin, ray.direction, out RaycastHit hit, maxDepth, layerMask))
        {
            Debug.Log("Hit " + hit.collider.name);
            Debug.DrawRay(shiftedOrigin, ray.direction * maxDepth, Color.green, 5f);
        }
        else
        {
            Debug.DrawRay(shiftedOrigin, ray.direction * maxDepth, Color.red, 5f);
        }

        yield return null;
    }
}
