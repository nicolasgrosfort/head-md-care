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
        public LeafAerodynamics aerodynamics;
        public Transform initialParent;
        public Vector3 initialLocalPosition;
        public Quaternion initialLocalRotation;
        public Vector3 initialScale;
        public GameObject spawnedFlower;
        public Animator flowerAnimator;
        public bool hasFallen;
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
            // linearDamping et angularDamping sont gérés par LeafAerodynamics.Awake.
            // Ne pas les définir ici pour éviter un conflit.

            if (!child.GetComponent<Collider>())
                child.gameObject.AddComponent<BoxCollider>();

            Renderer rend = child.GetComponent<Renderer>();
            if (rend != null && leafMaterial != null)
                rend.sharedMaterial = leafMaterial;

            // ── Stocker la ref avant AddComponent pour pouvoir la lier à LeafData ──
            var data = new LeafData
            {
                gameObject = child.gameObject,
                transform = child,
                rb = rb,
                initialParent = child.parent,
                initialLocalPosition = child.localPosition,
                initialLocalRotation = child.localRotation,
                initialScale = child.localScale,
            };

            // LeafAerodynamics.Awake s'exécute immédiatement et configure rb.linearDamping = 0.
            data.aerodynamics = child.gameObject.AddComponent<LeafAerodynamics>();

            _allLeaves.Add(data);
        }
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

        List<LeafData> fallenLeaves = _allLeaves.FindAll(l => l.hasFallen);

        // Mélange Fisher-Yates pour un ordre de repousse aléatoire.
        for (int i = fallenLeaves.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (fallenLeaves[i], fallenLeaves[j]) = (fallenLeaves[j], fallenLeaves[i]);
        }

        int toRegrow = Mathf.RoundToInt(fallenLeaves.Count * gameState.life);

        for (int i = 0; i < fallenLeaves.Count; i++)
        {
            bool shouldGrow = i < toRegrow;
            StartCoroutine(SpringRoutine(fallenLeaves[i], Random.Range(0f, 5f), shouldGrow));
        }
    }

    private void OnSummer(int dayNight)
    {
        if (dayNight == 0)
            return;

        foreach (var leaf in _allLeaves)
            if (leaf.flowerAnimator != null)
                StartCoroutine(KillFlowerRoutine(leaf, Random.Range(0f, 5f)));
    }

    private void OnFall(int dayNight)
    {
        if (dayNight == 0)
            return;

        foreach (var leaf in _allLeaves)
        {
            if (!leaf.gameObject.activeSelf)
                continue;
            StartCoroutine(FallRoutine(leaf, Random.Range(fallDelayMin, fallDelayMax)));
        }
    }

    private void OnWinter(int dayNight)
    {
        if (dayNight == gameState.Day)
            return;

        foreach (var leaf in _allLeaves)
        {
            if (!leaf.gameObject.activeSelf)
                continue;
            StartCoroutine(ShrinkRoutine(leaf, Random.Range(fallDelayMin, fallDelayMax)));
        }
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
        float duration = Random.Range(2f, 5f);
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

    private IEnumerator ShrinkRoutine(LeafData leaf, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!leaf.gameObject.activeSelf)
            yield break;

        float duration = 2f;
        float elapsed = 0f;
        Vector3 startScale = leaf.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            leaf.transform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                Mathf.SmoothStep(0f, 1f, elapsed / duration)
            );
            yield return null;
        }

        leaf.transform.localScale = Vector3.zero;
        leaf.gameObject.SetActive(false);
        leaf.transform.localScale = leaf.initialScale; // reset pour le prochain printemps
    }

    /// <summary>
    /// Lance la chute physique de la feuille.
    /// L'aérodynamique est entièrement gérée par LeafAerodynamics.FixedUpdate ;
    /// on se contente d'une légère perturbation angulaire initiale pour briser
    /// la symétrie et déclencher le flottement.
    /// </summary>
    private IEnumerator FallRoutine(LeafData leaf, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!leaf.gameObject.activeSelf)
            yield break;

        leaf.hasFallen = true;
        leaf.transform.SetParent(null);
        leaf.rb.isKinematic = false;

        // Petite impulsion angulaire aléatoire pour briser la symétrie initiale.
        // Pas de force linéaire : l'aérodynamique gère la dérive naturellement.
        Vector3 tiltAxis = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        leaf.rb.AddTorque(tiltAxis * 0.02f, ForceMode.VelocityChange);
    }

    private IEnumerator KillFlowerRoutine(LeafData leaf, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (leaf.flowerAnimator != null)
            leaf.flowerAnimator.SetTrigger("Dead");
    }

    private IEnumerator SpringRoutine(LeafData leaf, float delay, bool shouldGrow)
    {
        yield return new WaitForSeconds(delay);

        if (shouldGrow)
        {
            leaf.hasFallen = false;

            // Vider la vélocité accumulée pendant la chute avant de redevenir cinématique.
            leaf.rb.linearVelocity = Vector3.zero;
            leaf.rb.angularVelocity = Vector3.zero;
            leaf.rb.isKinematic = true;

            leaf.transform.SetParent(leaf.initialParent);
            leaf.transform.localPosition = leaf.initialLocalPosition;
            leaf.transform.localRotation = leaf.initialLocalRotation;
            leaf.gameObject.SetActive(true);
            StartCoroutine(GrowRoutine(leaf));
        }
        else
        {
            // Pas de repousse → fleur à la place.
            SpawnFlower(leaf);
        }
    }
}
