using System.Collections;
using System.Collections.Generic;
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
    public float windForce = 100f;
    public float windVariation = 40f;
    public float maxDepth = 200f;
    public float maxDistance = 10f;
    public LayerMask layerMask = ~0;
    public float wiggleIntensity = 6f;

    [SerializeField]
    private ParticleSystem windParticleSystem;

    [SerializeField]
    private Material rippleMaterial;

    public string leafTag = "Leaf";
    public string flowerTag = "Flower";

    private GameObject[] gameLeaves;
    private GameObject[] gameFlowers = new GameObject[0];
    private List<GameObject> gameObjects = new List<GameObject>();

    private Dictionary<GameObject, Quaternion> originalRotations =
        new Dictionary<GameObject, Quaternion>();

    private Coroutine blowCoroutine;
    private Coroutine rippleCoroutine;

    [Header("Debug")]
    [SerializeField]
    private bool debugMode = false;

    void Awake()
    {
        gameLeaves = GameObject.FindGameObjectsWithTag(leafTag);
        foreach (GameObject leaf in gameLeaves)
            originalRotations[leaf] = leaf.transform.localRotation;

        gameObjects = new List<GameObject>(gameLeaves);
    }

    void OnEnable()
    {
        gameState.OnClick += OnClick;
        gameState.OnDrag += OnDrag;
        gameState.OnGerminationEnd += InitializeFlowers;
    }

    void OnDisable()
    {
        gameState.OnClick -= OnClick;
        gameState.OnDrag -= OnDrag;
        gameState.OnGerminationEnd -= InitializeFlowers;
    }

    void OnClick(PointerEventData eventData)
    {
        HandleBlow(eventData);
    }

    void OnDrag(PointerEventData eventData)
    {
        HandleBlow(eventData);
    }

    private void InitializeFlowers()
    {
        gameFlowers = GameObject.FindGameObjectsWithTag(flowerTag);
        foreach (GameObject flower in gameFlowers)
            originalRotations[flower] = flower.transform.localRotation;

        gameObjects = new List<GameObject>(gameLeaves);
        gameObjects.AddRange(gameFlowers);

        Debug.Log("Initialized " + gameFlowers.Length + " flowers.");
    }

    private void HandleBlow(PointerEventData eventData)
    {
        if (blowCoroutine != null)
        {
            StopCoroutine(blowCoroutine);
        }

        blowCoroutine = StartCoroutine(Blow(eventData));

        if (rippleCoroutine != null)
            StopCoroutine(rippleCoroutine);

        Vector2 uv = new Vector2(
            eventData.position.x / Screen.width,
            eventData.position.y / Screen.height
        );

        rippleCoroutine = StartCoroutine(AnimateRipple(uv, 0.1f));
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

            foreach (GameObject leaf in gameObjects)
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
                float magnitudeNormalized = Mathf.Clamp(eventData.delta.magnitude / 100f, 1f, 2f);

                float effect =
                    (1f - distanceNormalized)
                    * (1f - depthNormalized)
                    * magnitudeNormalized
                    * intensity;

                if (debugMode)
                {
                    leaf.GetComponent<Renderer>().material.color = Color.Lerp(
                        Color.green,
                        Color.red,
                        effect
                    );
                }

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
                    if (leaf.CompareTag(flowerTag))
                    {
                        Wiggle(leaf, originalRotations[leaf], effect, t, 4f);
                    }
                    else if (leafRigidbody.isKinematic)
                    {
                        Wiggle(leaf, originalRotations[leaf], effect, t, 1f);
                    }
                    else
                    {
                        gameState.DecreaseLife(0.2f * effect * Time.deltaTime);
                        leafRigidbody.AddForce(forceDirection * Time.deltaTime, ForceMode.Impulse);
                        leafRigidbody.AddTorque(
                            torqueDirection * Time.deltaTime,
                            ForceMode.Impulse
                        );
                    }
                }
            }

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"gameLeaves length: {gameLeaves.Length}");

        windParticleSystem.gameObject.SetActive(false);
        yield return null;
    }

    private IEnumerator AnimateRipple(Vector2 center, float maxRadius)
    {
        rippleMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0, 0));

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float waveFront = Mathf.Lerp(0f, maxRadius, smoothT);
            float waveWidth = Mathf.Lerp(0.01f, maxRadius * 0.2f, smoothT);

            float amplitude =
                t < 0.1f
                    ? Mathf.Lerp(0f, 0.006f, t / 0.1f)
                    : Mathf.Lerp(0.006f, 0f, (t - 0.1f) / 0.9f);

            rippleMaterial.SetFloat("_Time2", elapsed * 10f);
            rippleMaterial.SetFloat("_Strength", amplitude);
            rippleMaterial.SetFloat("_WaveFront", waveFront);
            rippleMaterial.SetFloat("_WaveWidth", waveWidth);

            yield return null;
        }

        rippleMaterial.SetFloat("_Strength", 0f);
    }

    private void Wiggle(
        GameObject leaf,
        Quaternion initialRotation,
        float intensity,
        float t,
        float multiplier
    )
    {
        float fade = wiggleIntensity * intensity * (1f - t) * multiplier;
        float angleX = Mathf.Sin(Time.time * 30f) * fade;
        float angleY = Mathf.Sin(Time.time * 20f) * fade * 0.2f;
        float angleZ = Mathf.Sin(Time.time * 10f) * fade * 0.3f;
        leaf.transform.localRotation = initialRotation * Quaternion.Euler(angleX, angleY, angleZ);
    }
}
