using UnityEngine;

public class ButterflyFlight : MonoBehaviour
{
    [Header("Zone de vol")]
    [SerializeField]
    private Vector3 sphereCenter; // offset en local par rapport au point de départ

    [SerializeField]
    private float sphereRadius = 4f;

    [Header("Mouvement")]
    [SerializeField]
    private float moveSpeed = 2f;

    [SerializeField]
    private float rotationSpeed = 3f;

    [SerializeField]
    private float waypointReachDistance = 0.3f;

    [Header("Variation organique")]
    [SerializeField]
    private float noiseStrength = 0.1f;

    [SerializeField]
    private float noiseSpeed = 10f;

    [Header("Animation (battement d'ailes)")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float baseFlapSpeed = 1.5f;

    [SerializeField]
    private float speedMultiplier = 0.5f;

    private Vector3 origin;
    private Vector3 currentTarget;
    private float noiseOffsetX,
        noiseOffsetY,
        noiseOffsetZ;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        origin = transform.position;
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);
        noiseOffsetZ = Random.Range(0f, 100f);

        PickNewTarget();
    }

    void OnEnable()
    {
        if (origin == Vector3.zero)
            origin = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        // Si trop proche de la cible, en choisir une nouvelle
        if (Vector3.Distance(transform.position, currentTarget) < waypointReachDistance)
        {
            PickNewTarget();
        }

        // Direction vers la cible
        Vector3 direction = (currentTarget - transform.position).normalized;

        // Bruit pour un mouvement moins linéaire
        float t = Time.time * noiseSpeed;
        Vector3 noise =
            new Vector3(
                (Mathf.PerlinNoise(t, noiseOffsetX) - 0.5f),
                (Mathf.PerlinNoise(t, noiseOffsetY) - 0.5f),
                (Mathf.PerlinNoise(t, noiseOffsetZ) - 0.5f)
            ) * noiseStrength;

        Vector3 moveDir = (direction + noise).normalized;

        // Déplacement
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        // Rotation douce vers la direction de déplacement
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        // Vitesse de battement d'ailes liée au déplacement
        if (animator != null)
        {
            float currentSpeed = moveDir.magnitude * moveSpeed;
            animator.speed = baseFlapSpeed + currentSpeed * speedMultiplier;
        }
    }

    private void PickNewTarget()
    {
        Vector3 randomPoint = Random.insideUnitSphere * sphereRadius;
        currentTarget = origin + sphereCenter + randomPoint;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0f, 0.3f);
        Vector3 center = (Application.isPlaying ? origin : transform.position) + sphereCenter;
        Gizmos.DrawWireSphere(center, sphereRadius);
    }
}
