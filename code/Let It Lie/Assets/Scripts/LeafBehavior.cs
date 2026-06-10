using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class LeafBehaviour : MonoBehaviour
{
    // ── Paramètres exposés dans l'Inspector ───────────────────────────────────

    [Header("Physique")]
    [Tooltip("Résistance à l'air de base (drag linéaire).")]
    [Range(0.5f, 5f)]
    public float baseDrag = 2.5f;

    [Tooltip("Variation aléatoire appliquée au drag à chaque spawn.")]
    [Range(0f, 1f)]
    public float dragVariance = 0.5f;

    [Tooltip("Résistance en rotation.")]
    [Range(0f, 1f)]
    public float angularDrag = 0.8f;

    [Tooltip("Masse de la feuille (kg).")]
    [Range(0.001f, 0.1f)]
    public float mass = 0.01f;

    [Header("Impulsion latérale")]
    [Tooltip("Force horizontale max appliquée au spawn pour simuler le vent.")]
    [Range(0f, 2f)]
    public float lateralImpulse = 0.5f;

    [Tooltip("Force de rotation max appliquée au spawn.")]
    [Range(0f, 1f)]
    public float torqueImpulse = 0.3f;

    [Header("Détection de pose")]
    [Tooltip("Vitesse en dessous de laquelle la feuille est considérée comme posée.")]
    [Range(0.01f, 0.5f)]
    public float restVelocityThreshold = 0.05f;

    [Tooltip("Durée (sec) sous le seuil de vitesse avant de confirmer la pose.")]
    [Range(0.1f, 2f)]
    public float restConfirmDuration = 0.4f;

    public float dampingOnClick = 50f;
    private float slowDrag = 0f;
    public GameState gameState;

    [Header("Fleur")]
    public GameObject flowerPrefab;

    // ── Événement ─────────────────────────────────────────────────────────────

    /// <summary>Déclenché une fois quand la feuille s'est posée.</summary>
    public event Action<LeafBehaviour> OnRested;

    // ── État interne ──────────────────────────────────────────────────────────

    private Rigidbody _rb;
    private bool _hasLanded;
    private float _restTimer;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _hasLanded = false;
        _restTimer = 0f;
    }

    private void FixedUpdate()
    {
        if (_hasLanded)
            return;

        float t = Mathf.InverseLerp(
            gameState.defaultTimeSpeed,
            gameState.maxTimeSpeed,
            gameState.timeSpeed
        );

        _rb.AddForce(Vector3.down * Mathf.Lerp(1f, 50f, t), ForceMode.Acceleration);

        // Applique le slowDrag accumulé au clic
        if (slowDrag > 0f)
        {
            Vector3 dragForce = -_rb.linearVelocity * slowDrag;
            _rb.AddForce(dragForce, ForceMode.Acceleration);
        }

        CheckIfRested();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par le spawner juste après avoir activé l'objet.
    /// Applique des forces aléatoires pour varier la chute.
    /// </summary>
    public void Init()
    {
        _rb.WakeUp();
        _rb.mass = mass;
        _rb.linearDamping = baseDrag + UnityEngine.Random.Range(-dragVariance, dragVariance);
        _rb.angularDamping = angularDrag;

        // Légère dérive horizontale (simule un souffle d'air)
        Vector3 lateral = new Vector3(
            UnityEngine.Random.Range(-lateralImpulse, lateralImpulse),
            0f,
            UnityEngine.Random.Range(-lateralImpulse, lateralImpulse)
        );

        Vector3 dragForce = -_rb.linearVelocity * slowDrag;
        _rb.AddForce(dragForce, ForceMode.Acceleration);

        _rb.AddForce(lateral, ForceMode.Impulse);
        _rb.AddTorque(UnityEngine.Random.insideUnitSphere * torqueImpulse, ForceMode.Impulse);
    }

    // ── Détection de pose ─────────────────────────────────────────────────────

    private void CheckIfRested()
    {
        bool isAlmostStill =
            _rb.linearVelocity.magnitude < restVelocityThreshold
            && _rb.angularVelocity.magnitude < restVelocityThreshold;

        if (isAlmostStill)
        {
            _restTimer += Time.fixedDeltaTime;
            if (_restTimer >= restConfirmDuration)
                ConfirmRested();
        }
        else
        {
            _restTimer = 0f;
        }
    }

    private void ConfirmRested()
    {
        _hasLanded = true;
        _rb.Sleep(); // coupe la simulation physique → 0 overhead une fois posée
        OnRested?.Invoke(this);
    }

    public void ApplySlowBomb(float intensity = 1f)
    {
        slowDrag = Mathf.Min(slowDrag + dampingOnClick * intensity, 20f);
    }

    public void TransformToFlower()
    {
        if (flowerPrefab == null)
        {
            Debug.LogWarning("flowerPrefab non assigné !");
            return;
        }

        // Position de la feuille
        Vector3 spawnPosition = transform.position;

        // Rotation basée sur la normale de la feuille (axe Y vers le haut de la feuille)
        Quaternion spawnRotation = transform.rotation;

        // Instancie la fleur
        GameObject flower = Instantiate(flowerPrefab, spawnPosition, spawnRotation);

        // Désactive la feuille
        gameObject.SetActive(false);
    }

    public bool IsLanded() => _hasLanded;
}
