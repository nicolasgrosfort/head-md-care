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
    private Material leafMaterial;

    [Header("Flower")]
    [SerializeField]
    private GameObject flowerPrefab;

    private class LeafData
    {
        public GameObject gameObject;
        public Transform transform;
        public Rigidbody rb;
        public Transform initialParent;
        public Vector3 initialLocalPosition;
        public Quaternion initialLocalRotation;
        public Vector3 initialScale;
        public GameObject spawnedFlower;
        public Animator flowerAnimator;
    }

    private List<LeafData> _allLeaves = new();

    private void OnEnable() => gameState.OnDayNightChange += HandleDayNightChange;

    private void OnDisable() => gameState.OnDayNightChange -= HandleDayNightChange;

    private void Awake()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (!child.name.StartsWith(leafPrefix))
                continue;

            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb == null)
                rb = child.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.mass = 0.01f;
            rb.linearDamping = 3f;
            rb.angularDamping = 0.5f;

            if (!child.GetComponent<Collider>())
                child.gameObject.AddComponent<BoxCollider>();

            Renderer rend = child.GetComponent<Renderer>();
            if (rend != null && leafMaterial != null)
                rend.sharedMaterial = leafMaterial;

            _allLeaves.Add(
                new LeafData
                {
                    gameObject = child.gameObject,
                    transform = child,
                    rb = rb,
                    initialParent = child.parent,
                    initialLocalPosition = child.localPosition,
                    initialLocalRotation = child.localRotation,
                    initialScale = child.localScale,
                }
            );
        }
    }

    private void Update()
    {
        foreach (var leaf in _allLeaves)
        {
            if (leaf.rb.isKinematic)
                continue;

            // Vector3 velocity = leaf.rb.linearVelocity;
            // leaf.rb.AddForce(-velocity.normalized * velocity.sqrMagnitude * 0.1f, ForceMode.Force);
            // leaf.rb.AddTorque(Random.insideUnitSphere * 0.05f, ForceMode.Force);

            // Vector2 turbulence2D = Random.insideUnitCircle * gameState.windTurbulence;
            // Vector3 wind = gameState.windForce + new Vector3(turbulence2D.x, 0f, turbulence2D.y);
            // leaf.rb.AddForce(wind, ForceMode.Force);
        }
    }

    // ─── Wither ───────────────────────────────────────────────────────────────

    public void Wither(float percentage)
    {
        foreach (var leaf in _allLeaves)
            leaf.gameObject.SetActive(true);

        int toHide = Mathf.RoundToInt(_allLeaves.Count * (1f - percentage));
        List<LeafData> shuffled = new(_allLeaves);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < toHide; i++)
            shuffled[i].gameObject.SetActive(false);
    }

    // ─── Saisons ──────────────────────────────────────────────────────────────

    private void HandleDayNightChange(int dayNight, int season)
    {
        switch (season)
        {
            case 0:
                OnSpring(dayNight);
                break;
            case 1:
                OnSummer(dayNight);
                break;
            case 2:
                OnFall(dayNight);
                break;
            case 3:
                OnWinter(dayNight);
                break;
        }
    }

    private void OnSpring(int dayNight)
    {
        if (dayNight == 0)
            return;

        foreach (var leaf in _allLeaves)
        {
            SpawnFlower(leaf);
            leaf.rb.isKinematic = true;
            leaf.transform.SetParent(leaf.initialParent);
            leaf.transform.localPosition = leaf.initialLocalPosition;
            leaf.transform.localRotation = leaf.initialLocalRotation;
            leaf.gameObject.SetActive(true);
            StartCoroutine(GrowRoutine(leaf));
        }

        Wither(gameState.life);
    }

    private void OnSummer(int dayNight)
    {
        if (dayNight == 0)
            return;

        foreach (var leaf in _allLeaves)
            if (leaf.flowerAnimator != null)
                leaf.flowerAnimator.SetTrigger("Dead");
    }

    private void OnFall(int dayNight)
    {
        if (dayNight == 0)
            return;

        foreach (var leaf in _allLeaves)
        {
            if (!leaf.gameObject.activeSelf)
                continue;

            leaf.transform.SetParent(null);
            leaf.rb.isKinematic = false;

            Vector3 force = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.1f, 0f),
                Random.Range(-0.3f, 0.3f)
            );
            leaf.rb.AddForce(force, ForceMode.Impulse);
            leaf.rb.AddTorque(Random.insideUnitSphere * 0.3f, ForceMode.Impulse);
        }
    }

    private void OnWinter(int dayNight)
    {
        if (dayNight == 0)
            return;

        foreach (var leaf in _allLeaves)
            leaf.gameObject.SetActive(false);
    }

    // ─── Fleurs ───────────────────────────────────────────────────────────────

    private void SpawnFlower(LeafData leaf)
    {
        if (flowerPrefab == null)
            return;
        if (leaf.spawnedFlower != null)
            return;

        Vector3 pos = leaf.transform.position;
        Quaternion rot = leaf.transform.rotation;

        if (
            Physics.Raycast(
                leaf.transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out RaycastHit hit,
                0.5f
            )
        )
        {
            rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            pos = hit.point;
        }

        leaf.spawnedFlower = Instantiate(flowerPrefab, pos, rot);
        leaf.flowerAnimator = leaf.spawnedFlower.GetComponent<Animator>();
        leaf.gameObject.SetActive(false);
    }

    // ─── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator GrowRoutine(LeafData leaf)
    {
        float duration = 2f;
        float elapsed = 0f;
        leaf.transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            leaf.transform.localScale = Vector3.Lerp(
                Vector3.zero,
                leaf.initialScale,
                Mathf.SmoothStep(0f, 1f, elapsed / duration)
            );
            yield return null;
        }

        leaf.transform.localScale = leaf.initialScale;
    }
}
