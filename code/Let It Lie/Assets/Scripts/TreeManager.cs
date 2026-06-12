using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        gameState.OnClick -= OnClick;
    }

    private void OnClick(Vector2 screenPos) { }

    private void OnFallNight(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
        {
            if (!leaf.gameObject.activeSelf)
                continue;
            StartCoroutine(FallRoutine(leaf, Random.Range(fallDelayMin, fallDelayMax)));
        }
    }

    private void OnFallDay(int cycle, int season) { }

    private void OnWinterNight(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
        {
            if (!leaf.hasFallen || !leaf.gameObject.activeSelf)
                continue;

            StartCoroutine(HumificationRoutine(leaf, Random.Range(0f, 3f)));
        }
        return;
    }

    private void OnWinterDay(int cycle, int season)
    {
        if (worm != null)
            StartCoroutine(MoveWormRoutine());
    }

    private void OnSpringNight(int cycle, int season)
    {
        List<LeafData> fallenLeaves = Shuffle(_allLeaves.FindAll(l => l.hasFallen && l.hasBudding));

        Debug.Log(
            $"Spring Night: {fallenLeaves.Count} fallen leaves can potentially bud, all leaves: {_allLeaves.Count}"
        );

        for (int i = 0; i < fallenLeaves.Count; i++)
        {
            StartCoroutine(GerminationRoutine(fallenLeaves[i], Random.Range(0f, 5f)));
        }
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

    private void OnSummerNight(int cycle, int season) { }

    private void OnSummerDay(int cycle, int season)
    {
        foreach (var leaf in _allLeaves)
            if (leaf.flowerAnimator != null)
                StartCoroutine(WitherRoutine(leaf, Random.Range(0f, 5f)));
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
            Random.Range(-20f, 20f),
            Random.Range(0f, 360f),
            Random.Range(-20f, 20f)
        );
        Vector3 scale = Vector3.one * Random.Range(12f, 18f);

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
}
