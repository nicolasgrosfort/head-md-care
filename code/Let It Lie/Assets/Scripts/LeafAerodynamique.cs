using UnityEngine;

/// <summary>
/// Simule les forces aérodynamiques réalistes d'une feuille qui tombe, sans vent.
///
/// Modèle physique :
///
///   1. TRAÎNÉE ANISOTROPE (plaque plane)
///      Une plaque plane oppose une très forte résistance perpendiculairement à sa face
///      (la feuille « freine sur l'air »), mais une friction très faible parallèlement
///      (elle peut dériver latéralement).
///
///        F = −Cd_n · vₙ|vₙ| · n̂  −  Cd_t · vₜ|vₜ| · t̂
///
///      Cd_n >> Cd_t  →  chute lente à plat, glissement latéral en inclinaison.
///
///   2. COUPLE STABILISATEUR
///      Une feuille plate tombe naturellement face vers le haut (équilibre neutre).
///      Ce couple ramène doucement la normale vers Vector3.up.
///
///   3. COUPLE DE FLOTTEMENT
///      Une plaque en écoulement est aérodynamiquement instable et oscille
///      périodiquement. Modélisé par un couple sinusoïdal autour d'un axe
///      horizontal aléatoire, propre à chaque feuille.
///
/// Vitesse terminale théorique (feuille à plat) ≈ √(m·g / Cd_n) ≈ 1 m/s avec les valeurs par défaut.
///
/// IMPORTANT : le champ <b>normalAxis</b> doit correspondre à l'axe local perpendiculaire
/// à la face du mesh (par défaut transform.up).
///
/// Pour ajouter du vent plus tard : appelez <see cref="AddWindForce"/> depuis votre source de vent.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class LeafAerodynamics : MonoBehaviour
{
    // ── Axe normal ────────────────────────────────────────────────────────────

    private enum NormalAxis
    {
        Up,
        Down,
        Forward,
        Back,
        Right,
        Left,
    }

    [Header("Face de la feuille")]
    [
        SerializeField,
        Tooltip(
            "Axe local perpendiculaire à la face du mesh.\n"
                + "Ajuster si la feuille ne flotte pas ou tombe de travers.\n"
                + "Default : Up (transform.up)."
        )
    ]
    private NormalAxis normalAxis = NormalAxis.Up;

    // ── Traînée ───────────────────────────────────────────────────────────────

    [Header("Traînée aérodynamique")]
    [
        SerializeField,
        Tooltip(
            "Résistance perpendiculaire à la face de la feuille (principale).\n"
                + "Élevée = chute lente et flottante. v_terminal ≈ √(m·g / Cd_n)."
        )
    ]
    private float normalDragCoeff = 0.10f;

    [
        SerializeField,
        Tooltip(
            "Friction tangentielle (parallèle à la face).\n"
                + "Garder faible pour autoriser la dérive latérale."
        )
    ]
    private float tangentialDragCoeff = 0.008f;

    // ── Flottement ────────────────────────────────────────────────────────────

    [Header("Flottement")]
    [
        SerializeField,
        Tooltip(
            "Intensité du couple qui réaligne la feuille à l'horizontale.\n"
                + "Trop élevé : plaque rigide. Trop faible : culbute chaotique."
        )
    ]
    private float stabilisationTorque = 0.025f;

    [
        SerializeField,
        Tooltip("Amplitude de l'oscillation de basculement (rad/s par step physique).")
    ]
    private float flutterAmplitude = 0.018f;

    [
        SerializeField,
        Tooltip("Fréquence de base du flottement en Hz (±30 % aléatoire par feuille).")
    ]
    private float flutterFrequency = 1.2f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Rigidbody _rb;
    private float _phase; // phase initiale aléatoire
    private float _freq; // fréquence réelle de cette feuille
    private Vector3 _flutterAxis; // axe horizontal aléatoire pour l'oscillation

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // On gère la traînée manuellement → désactiver la traînée linéaire Unity.
        // Conserver un peu de traînée angulaire pour limiter la rotation permanente.
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0.4f;

        // Randomisation par feuille : évite que toutes les feuilles oscillent en phase.
        _phase = Random.Range(0f, Mathf.PI * 2f);
        _freq = flutterFrequency * Random.Range(0.7f, 1.3f);
        _flutterAxis = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
    }

    private void FixedUpdate()
    {
        // Rien à faire si le corps est cinématique ou s'il s'est endormi (feuille posée).
        if (_rb.isKinematic || _rb.IsSleeping())
            return;

        // Guard NaN/Inf : si PhysX a corrompu l'état du corps (ex. collision dégénérée),
        // ne pas appliquer de forces supplémentaires qui propageraient la corruption.
        if (!IsStateFinite())
            return;

        Vector3 leafNormal = GetLeafNormal();
        ApplyAerodynamicDrag(leafNormal);
        ApplyStabilisationTorque(leafNormal);
        ApplyFlutterTorque();
    }

    // ── Forces ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Traînée de plaque plane : décompose la vitesse en composante normale (à travers la face)
    /// et tangentielle (le long de la face), puis applique des coefficients distincts.
    ///
    ///   F = −Cd_n · vₙ|vₙ| · n̂  −  Cd_t · vₜ|vₜ| · t̂
    ///
    /// Cette asymétrie produit la chute lente + dérive pendulaire caractéristique.
    /// </summary>
    private void ApplyAerodynamicDrag(Vector3 normal)
    {
        Vector3 vel = _rb.linearVelocity;

        // Projection de la vitesse sur la normale et sur le plan tangent.
        float vn = Vector3.Dot(vel, normal);
        Vector3 velNormal = normal * vn;
        Vector3 velTangent = vel - velNormal;

        // Traînée quadratique : F ∝ v² (plus physique qu'une traînée linéaire).
        Vector3 force =
            -velNormal * (Mathf.Abs(vn) * normalDragCoeff)
            - velTangent * (velTangent.magnitude * tangentialDragCoeff);

        _rb.AddForce(force, ForceMode.Force);
    }

    /// <summary>
    /// Stabilité aérodynamique neutre : la feuille veut que sa face soit horizontale.
    /// Le couple correcteur est proportionnel à l'écart angulaire (cross product).
    /// </summary>
    private void ApplyStabilisationTorque(Vector3 normal)
    {
        Vector3 correction = Vector3.Cross(normal, Vector3.up);
        _rb.AddTorque(correction * stabilisationTorque, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Instabilité périodique : oscillation sinusoïdale autour d'un axe horizontal aléatoire.
    /// Reproduit le mouvement de « flottement » visible sur une vraie feuille.
    /// </summary>
    private void ApplyFlutterTorque()
    {
        float s = Mathf.Sin(Time.fixedTime * _freq * Mathf.PI * 2f + _phase);
        _rb.AddTorque(_flutterAxis * (s * flutterAmplitude), ForceMode.VelocityChange);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par une source de vent externe (ex. sphère trigger).
    /// Appeler à chaque FixedUpdate depuis la source pour un effet continu.
    /// La force est déjà en Newtons — l'atténuation par distance est à gérer côté source.
    /// </summary>
    public void AddWindForce(Vector3 force)
    {
        if (_rb.isKinematic)
            return;
        _rb.AddForce(force, ForceMode.Force);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie que l'état physique du corps est sain (pas de NaN ni d'infini).
    /// Un MeshCollider convex dégénéré (scale → 0) peut corrompre l'état PhysX ;
    /// ce guard évite de propager la corruption en appliquant des forces sur un NaN.
    /// </summary>
    private bool IsStateFinite()
    {
        Vector3 vel = _rb.linearVelocity;
        Vector3 ang = _rb.angularVelocity;
        return float.IsFinite(vel.x)
            && float.IsFinite(vel.y)
            && float.IsFinite(vel.z)
            && float.IsFinite(ang.x)
            && float.IsFinite(ang.y)
            && float.IsFinite(ang.z);
    }

    private Vector3 GetLeafNormal() =>
        normalAxis switch
        {
            NormalAxis.Up => transform.up,
            NormalAxis.Down => -transform.up,
            NormalAxis.Forward => transform.forward,
            NormalAxis.Back => -transform.forward,
            NormalAxis.Right => transform.right,
            NormalAxis.Left => -transform.right,
            _ => transform.up,
        };
}
