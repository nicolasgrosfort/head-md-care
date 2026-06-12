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
    public float windDuration = 10f;
    public float maxDepth = 200f;
    public float mayDistance = 20f;
    public LayerMask layerMask = ~0;
    private GameObject[] gameLeaves;

    private Coroutine blowCoroutine;

    // public float force = 10f;
    // public float dureeImpulsion = 0.5f;

    void Awake()
    {
        gameLeaves = GameObject.FindGameObjectsWithTag("Leaf");
        Debug.Log("Found " + gameLeaves.Length + " leaves.");
    }

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
        HandleBlow(screenPos);
    }

    void OnDrag(Vector2 screenPos)
    {
        HandleBlow(screenPos);
    }

    private void HandleBlow(Vector2 screenPos)
    {
        if (blowCoroutine != null)
        {
            StopCoroutine(blowCoroutine);
        }

        blowCoroutine = StartCoroutine(Blow(screenPos));
    }

    IEnumerator Blow(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Vector3 origin = ray.origin + ray.direction * windOrigin;
        Vector3 direction = ray.direction;

        Debug.DrawRay(origin, direction * maxDepth, Color.red, 5f);

        float timeElapsed = 0f;

        while (timeElapsed < windDuration)
        {
            float t = timeElapsed / windDuration;
            float intensity = 1f - t;

            foreach (GameObject leaf in gameLeaves)
            {
                Vector3 position = leaf.transform.position;
                Vector3 toLeaf = position - origin;

                float depth = Vector3.Dot(toLeaf, direction);
                float distance = Vector3.Cross(toLeaf, direction).magnitude;

                if (depth < 0 || depth > maxDepth)
                {
                    continue;
                }

                float distanceNormalized = distance / mayDistance;
                float depthNormalized = depth / maxDepth;

                float effect = (1f - distanceNormalized) * (1f - depthNormalized) * intensity;

                leaf.GetComponent<Renderer>().material.color = Color.Lerp(
                    Color.green,
                    Color.red,
                    effect
                );
            }

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"gameLeaves length: {gameLeaves.Length}");

        yield return null;
    }
}
