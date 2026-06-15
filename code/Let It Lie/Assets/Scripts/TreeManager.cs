using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class TreeManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField]
    private GameState gameState;

    [Header("Leafs")]
    [SerializeField]
    private string leafPrefix = "Leaf";

    [SerializeField]
    private float fallDelayMin = 0f;

    [SerializeField]
    private float fallDelayMax = 3f;

    [SerializeField]
    private Color springColor = Color.green;

    [SerializeField]
    private Color summerColor = Color.yellow;

    [SerializeField]
    private Color fallColor = Color.brown;

    [SerializeField]
    private Color winterColor = Color.white;

    [Header("Fog")]
    [SerializeField]
    private ParticleSystem fogParticle;

    [SerializeField]
    private float fogTransitionDuration = 4f;

    [SerializeField]
    private float minFogEmission = 0f;

    [SerializeField]
    private float maxFogEmission = 30f;
    private ParticleSystem.EmissionModule fogEmission;

    [Header("Flower")]
    [SerializeField]
    private GameObject flowerPrefab;

    [SerializeField]
    private FlowerZone[] zones;

    [SerializeField]
    private Material[] flowerColorVariants;

    [Header("Worm")]
    [SerializeField]
    private Transform worm;

    [SerializeField]
    private float wormMoveDuration = 3f;

    [Header("Butterfly")]
    [SerializeField]
    private GameObject butterflyPrefab;

    private class LeafData
    {
        public GameObject gameObject;
        public Transform transform;
        public Rigidbody rb;
        public LeafAerodynamics aerodynamics;
        public Vector3 initialLocalPosition;
        public Transform initialParent;
        public Quaternion initialLocalRotation;
        public Vector3 initialScale;
        public GameObject spawnedFlower;
        public Animator flowerAnimator;

        public bool hasFallen = false;
        public bool hasBudding = true;
    }

    private List<LeafData> _allLeaves = new();

    private void Awake()
    {
        fogEmission = fogParticle.emission;

        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (!child.name.StartsWith(leafPrefix))
                continue;

            var leaf = new LeafData
            {
                gameObject = child.gameObject,
                transform = child,
                initialParent = child.parent,
                rb = child.GetComponent<Rigidbody>(),
                initialLocalPosition = child.localPosition,
                initialLocalRotation = child.localRotation,
                initialScale = child.localScale,
            };

            _allLeaves.Add(leaf);
        }
    }

    private void OnEnable()
    {
        gameState.OnFallNight += OnFallNight;
        gameState.OnFallDay += OnFallDay;
        gameState.OnSpringNight += OnSpringNight;
        gameState.OnSpringDay += OnSpringDay;
        gameState.OnSummerNight += OnSummerNight;
        gameState.OnSummerDay += OnSummerDay;
        gameState.OnWinterNight += OnWinterNight;
        gameState.OnWinterDay += OnWinterDay;
        gameState.OnLifeChange += HandleLifeChange;
        gameState.OnClick += OnClick;
    }

    private void OnDisable()
    {
        gameState.OnFallNight -= OnFallNight;
        gameState.OnFallDay -= OnFallDay;
        gameState.OnSpringNight -= OnSpringNight;
        gameState.OnSpringDay -= OnSpringDay;
        gameState.OnSummerNight -= OnSummerNight;
        gameState.OnSummerDay -= OnSummerDay;
        gameState.OnWinterNight -= OnWinterNight;
        gameState.OnWinterDay -= OnWinterDay;
        gameState.OnLifeChange -= HandleLifeChange;
        gameState.OnClick -= OnClick;
    }

    private void OnClick(PointerEventData eventData) { }

    private void HandleLifeChange(float life)
    {
        foreach (var leaf in _allLeaves)
        {
            if (!leaf.gameObject.activeSelf)
                continue;

            Color color = leaf.gameObject.GetComponent<Renderer>().material.color;
            float alpha = Mathf.Clamp(gameState.life, 0.2f, 0.8f);
            leaf.gameObject.GetComponent<Renderer>().material.color = new Color(
                color.r,
                color.g,
                color.b,
                alpha
            );
        }
    }

    private void OnFallNight(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
        {
            if (!leaf.gameObject.activeSelf)
                continue;

            StartCoroutine(FallRoutine(leaf, Random.Range(fallDelayMin, fallDelayMax)));
        }
    }

    private void OnFallDay(int cycle, int season)
    {
        butterflyPrefab.SetActive(false);
    }

    private void OnWinterNight(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
        {
            if (!leaf.hasFallen || !leaf.gameObject.activeSelf)
                continue;

            StartCoroutine(HumificationRoutine(leaf, Random.Range(0f, 3f)));
        }

        StartCoroutine(
            FogTransition(fogEmission, minFogEmission, maxFogEmission, fogTransitionDuration)
        );
    }

    private void OnWinterDay(int cycle, int season)
    {
        if (worm != null)
            StartCoroutine(MoveWormRoutine());
    }

    private void OnSpringNight(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
        {
            leaf.gameObject.GetComponent<Renderer>().material.color = new Color(
                springColor.r,
                springColor.g + Random.Range(-0.2f, 0.2f),
                springColor.b,
                springColor.a
            );
        }

        List<LeafData> fallenLeaves = Shuffle(_allLeaves.FindAll(l => l.hasFallen && l.hasBudding));

        StartCoroutine(GerminationAndNotify(fallenLeaves));

        StartCoroutine(
            FogTransition(fogEmission, maxFogEmission, minFogEmission, fogTransitionDuration)
        );
    }

    private void OnSpringDay(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
        {
            leaf.hasBudding = false;
        }

        List<LeafData> nextLeaves = TakePercent(
            _allLeaves.FindAll(l => l.hasFallen),
            gameState.life * 100f
        );

        for (int i = 0; i < nextLeaves.Count; i++)
        {
            StartCoroutine(BuddingRoutine(nextLeaves[i], Random.Range(0f, 5f)));
        }
    }

    private void OnSummerNight(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
        {
            if (leaf.gameObject.activeSelf)
            {
                Color nextSummerColor = new Color(
                    summerColor.r + Random.Range(-0.2f, 0.2f),
                    summerColor.g,
                    summerColor.b,
                    summerColor.a
                );
                StartCoroutine(
                    ProgressiveColor(
                        leaf,
                        leaf.gameObject.GetComponent<Renderer>().material.color,
                        nextSummerColor,
                        Random.Range(0.5f, 4f)
                    )
                );
            }
        }

        if (gameState.life > 0.25f)
        {
            butterflyPrefab.SetActive(true);
        }
    }

    private void OnSummerDay(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
        {
            if (leaf.flowerAnimator != null)
                StartCoroutine(WitherRoutine(leaf, Random.Range(0f, 5f)));

            Color nextFallColor = new Color(
                fallColor.r + Random.Range(-0.2f, 0.2f),
                fallColor.g,
                fallColor.b + Random.Range(-0.2f, 0.2f),
                fallColor.a
            );
            StartCoroutine(
                ProgressiveColor(
                    leaf,
                    leaf.gameObject.GetComponent<Renderer>().material.color,
                    nextFallColor,
                    Random.Range(0.5f, 4f)
                )
            );
        }
    }

    // UTILS

    private List<T> Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    public List<T> TakePercent<T>(List<T> list, float percent, bool random = true)
    {
        if (random)
            list = Shuffle(list);

        int count = Mathf.CeilToInt(list.Count * percent / 100f);

        return list.GetRange(0, count);
    }

    // PRIVATE METHODS

    private Vector3 GetRandomSpawnPosition()
    {
        if (zones.Length == 0)
            return Vector3.zero;

        FlowerZone zone = zones[Random.Range(0, zones.Length)];
        return zone.GetRandomPosition();
    }

    private void ApplyRandomFlowerMaterial(GameObject flower)
    {
        if (flowerColorVariants == null || flowerColorVariants.Length == 0)
            return;

        var smr = flower.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
            return;

        Material chosen = flowerColorVariants[Random.Range(0, flowerColorVariants.Length)];

        Material[] mats = smr.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i].name.Contains("Flower"))
            {
                mats[i] = chosen;
                break;
            }
        }
        smr.sharedMaterials = mats;
    }

    // COROUTINES

    private IEnumerator GerminationAndNotify(List<LeafData> fallenLeaves)
    {
        List<Coroutine> coroutines = new List<Coroutine>();

        for (int i = 0; i < fallenLeaves.Count; i++)
        {
            coroutines.Add(
                StartCoroutine(GerminationRoutine(fallenLeaves[i], Random.Range(0f, 5f)))
            );
        }

        foreach (Coroutine c in coroutines)
            yield return c;

        gameState.TriggerGerminationEnd();
    }

    private IEnumerator HumificationRoutine(LeafData leaf, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!leaf.gameObject.activeSelf)
            yield break;

        leaf.gameObject.SetActive(false);
    }

    private IEnumerator FallRoutine(LeafData leaf, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!leaf.gameObject.activeSelf)
            yield break;

        leaf.hasFallen = true;
        leaf.rb.isKinematic = false;
        leaf.transform.SetParent(null);
    }

    private IEnumerator WitherRoutine(LeafData leaf, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (leaf.flowerAnimator == null)
            yield break;

        leaf.flowerAnimator.SetTrigger("Dead");

        AnimatorStateInfo state = leaf.flowerAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(state.length);

        if (leaf.spawnedFlower != null)
            Destroy(leaf.spawnedFlower);

        leaf.spawnedFlower = null;
        leaf.flowerAnimator = null;
    }

    private IEnumerator BuddingRoutine(LeafData leaf, float delay)
    {
        yield return new WaitForSeconds(delay);

        leaf.hasFallen = false;
        leaf.hasBudding = true;

        leaf.rb.linearVelocity = Vector3.zero;
        leaf.rb.angularVelocity = Vector3.zero;
        leaf.rb.isKinematic = true;

        leaf.transform.SetParent(leaf.initialParent);
        leaf.transform.localPosition = leaf.initialLocalPosition;
        leaf.transform.localRotation = leaf.initialLocalRotation;
        leaf.transform.localScale = leaf.initialScale;

        leaf.gameObject.SetActive(true);
    }

    private IEnumerator GerminationRoutine(LeafData leaf, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (
            leaf.gameObject.activeSelf
            || flowerPrefab == null
            || leaf.spawnedFlower != null
            || !leaf.hasBudding
        )
            yield break;

        Vector3 pos = GetRandomSpawnPosition();
        Quaternion rot = Quaternion.Euler(
            Random.Range(-25f, 25f),
            Random.Range(0f, 360f),
            Random.Range(-25f, 25f)
        );
        Vector3 scale = Vector3.one * Random.Range(5f, 10f);

        leaf.spawnedFlower = Instantiate(flowerPrefab, pos, rot);
        leaf.spawnedFlower.transform.localScale = scale;
        leaf.flowerAnimator = leaf.spawnedFlower.GetComponent<Animator>();

        ApplyRandomFlowerMaterial(leaf.spawnedFlower);
    }

    private IEnumerator MoveWormRoutine()
    {
        Vector3 startPos = worm.localPosition;
        Vector3 endPos = startPos;

        startPos.x = -30f;
        startPos.y = -18f;
        endPos.x = 30f;
        endPos.y = -30f;

        worm.localPosition = startPos;
        worm.localScale = Vector3.one * 0.5f * Mathf.Clamp(gameState.life, 0.5f, 1.5f);

        float elapsed = 0f;
        while (elapsed < wormMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / wormMoveDuration);
            worm.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        worm.localPosition = endPos;
    }

    private IEnumerator ProgressiveColor(
        LeafData leaf,
        Color startColor,
        Color endColor,
        float duration
    )
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            leaf.gameObject.GetComponent<Renderer>().material.color = Color.Lerp(
                startColor,
                endColor,
                t
            );

            yield return null;
        }
    }

    private IEnumerator FogTransition(
        ParticleSystem.EmissionModule fogEmission,
        float startRate,
        float endRate,
        float duration
    )
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fogEmission.rateOverTime = Mathf.Lerp(startRate, endRate, t);
            yield return null;
        }
    }
}
