using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class WindManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField]
    private GameState gameState;

    [Header("Wind")]
    [SerializeField]
    private float windOrigin = 50f;
    public float windDuration = 0.6f;
    public float windForce = 120f;
    public float windVariation = 40f;
    public float maxDepth = 200f;
    public float maxDistance = 10f;
    public LayerMask layerMask = ~0;

    [SerializeField]
    private ParticleSystem windParticleSystem;

    public string gameTag = "Leaf";
    private GameObject[] gameLeaves;

    private Coroutine blowCoroutine;

    void Awake()
    {
        gameLeaves = GameObject.FindGameObjectsWithTag(gameTag);
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

    void OnClick(PointerEventData eventData)
    {
        HandleBlow(eventData);
    }

    void OnDrag(PointerEventData eventData)
    {
        HandleBlow(eventData);
    }

    private void HandleBlow(PointerEventData eventData)
    {
        if (blowCoroutine != null)
        {
            StopCoroutine(blowCoroutine);
        }

        blowCoroutine = StartCoroutine(Blow(eventData));
    }

    IEnumerator Blow(PointerEventData eventData)
    {
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        Vector3 origin = ray.origin + ray.direction * windOrigin;
        Vector3 direction = ray.direction;

        windParticleSystem.gameObject.SetActive(true);
        windParticleSystem.transform.position = origin;

        Debug.DrawRay(origin, direction * maxDepth, Color.red, 5f);

        float timeElapsed = 0f;

        while (timeElapsed < windDuration)
        {
            float t = timeElapsed / windDuration;
            float intensity = 1f - t;

            windParticleSystem.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f) * intensity;

            foreach (GameObject leaf in gameLeaves)
            {
                Vector3 position = leaf.transform.position;
                Vector3 toLeaf = position - origin;

                float depth = Vector3.Dot(toLeaf, direction);
                float distance = Vector3.Cross(toLeaf, direction).magnitude;

                if (depth < 0 || depth > maxDepth)
                    continue;
                if (distance > maxDistance)
                    continue;

                float distanceNormalized = distance / maxDistance;
                float depthNormalized = depth / maxDepth;
                float deltaMM = eventData.delta.magnitude / Screen.dpi * 25.4f;
                float magnitudeNormalized = Mathf.Clamp(deltaMM / 10f, 1f, 2f);

                float effect =
                    (1f - distanceNormalized)
                    * (1f - depthNormalized)
                    * magnitudeNormalized
                    * intensity;

                // leaf.GetComponent<Renderer>().material.color = Color.Lerp(
                //     Color.green,
                //     Color.red,
                //     effect
                // );

                Vector3 forceDirection = direction.normalized * effect * windForce;
                Vector3 randomVariation = new Vector3(
                    Random.Range(-windVariation, windVariation),
                    Random.Range(-windVariation, windVariation),
                    Random.Range(-windVariation, windVariation)
                );
                forceDirection += randomVariation;

                Vector3 torqueDirection =
                    Vector3.Cross(Vector3.up, toLeaf).normalized * effect * windForce * 0.5f;
                Vector3 randomTorqueVariation = new Vector3(
                    Random.Range(-windVariation, windVariation),
                    Random.Range(-windVariation, windVariation),
                    Random.Range(-windVariation, windVariation)
                );
                torqueDirection += randomTorqueVariation;

                Rigidbody leafRigidbody = leaf.GetComponent<Rigidbody>();
                if (leafRigidbody != null)
                {
                    leafRigidbody.AddForce(forceDirection, ForceMode.Force);
                    leafRigidbody.AddTorque(torqueDirection, ForceMode.Force);
                }
            }

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"gameLeaves length: {gameLeaves.Length}");

        windParticleSystem.gameObject.SetActive(false);
        yield return null;
    }
}
