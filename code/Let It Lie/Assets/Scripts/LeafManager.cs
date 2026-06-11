using System.Collections;
using UnityEngine;

public class Leaf : MonoBehaviour
{
    private GameState gameState;
    private GameObject flowerPrefab;
    private Animator flowerAnimator;
    private GameObject _spawnedFlower;
    private Rigidbody _rb;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;

    private Vector3 _fallPosition;
    private Quaternion _fallRotation;
    private Transform _initialParent;

    private Vector3 _initialScale;

    public void Init(GameState state, GameObject prefab, Material leafMat = null)
    {
        gameState = state;
        gameState.OnDayNightChange += HandleDayNightChange;

        flowerPrefab = prefab;

        if (leafMat != null)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
                rend.sharedMaterial = leafMat;
        }

        HandleDayNightChange(gameState.season);
    }

    private void Update()
    {
        if (_rb.isKinematic)
            return;

        Vector3 velocity = _rb.linearVelocity;
        _rb.AddForce(-velocity.normalized * velocity.sqrMagnitude * 0.1f, ForceMode.Force);
        _rb.AddTorque(Random.insideUnitSphere * 0.05f, ForceMode.Force);

        // Turbulence uniquement horizontale
        Vector2 turbulence2D = Random.insideUnitCircle * gameState.windTurbulence;
        Vector3 turbulence = new Vector3(turbulence2D.x, 0f, turbulence2D.y);

        Vector3 wind = gameState.windForce + turbulence;
        _rb.AddForce(wind, ForceMode.Force);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_rb.isKinematic)
            return;

        // Ralentit progressivement au contact du sol
        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, Time.deltaTime * 2f);
        _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, Time.deltaTime * 2f);
    }

    private void OnDestroy()
    {
        if (gameState != null)
        {
            gameState.OnDayNightChange -= HandleDayNightChange;
        }
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;

        _initialScale = transform.localScale;

        _initialLocalPosition = transform.localPosition;
        _initialLocalRotation = transform.localRotation;
        _initialParent = transform.parent;
    }

    private void HandleDayNightChange(float time)
    {
        int dayNight = Mathf.FloorToInt(gameState.time / 0.5f);
        int season = Mathf.FloorToInt(gameState.season / 0.25f);

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

        Debug.Log($"DayNight change: dayNight={dayNight}, season={season}");
    }

    private void OnSpring(int dayNight)
    {
        if (dayNight == 0)
            return; // Ne rien faire la nuit

        SpawnFlower();

        _rb.isKinematic = true;
        transform.SetParent(_initialParent);
        transform.localPosition = _initialLocalPosition;
        transform.localRotation = _initialLocalRotation;
        gameObject.SetActive(true);
        StartCoroutine(GrowRoutine());
        // TODO: changer couleur → vert
    }

    private void OnSummer(int dayNight)
    {
        if (dayNight == 0)
            return;

        if (flowerAnimator != null)
            flowerAnimator.SetTrigger("Dead");
    }

    private IEnumerator DestroyAfterAnimation()
    {
        // Attend que l'animation démarre
        yield return null;

        // Attend la fin de l'animation
        AnimatorStateInfo stateInfo = flowerAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);

        Destroy(gameObject);
    }

    private void OnFall(int dayNight)
    {
        if (dayNight == 0)
            return; // Ne rien faire la nuit

        // Détache et active la physique
        transform.SetParent(null);
        _rb.isKinematic = false;

        // Impulsion légère vers le bas + côté aléatoire
        Vector3 force = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(-0.1f, 0f),
            Random.Range(-0.3f, 0.3f)
        );
        _rb.AddForce(force, ForceMode.Impulse);

        // Rotation initiale aléatoire
        _rb.AddTorque(Random.insideUnitSphere * 0.3f, ForceMode.Impulse);
    }

    private void OnWinter(int dayNight)
    {
        if (dayNight == 0)
            return; // Ne rien faire la nuit

        gameObject.SetActive(false);
    }

    private void SpawnFlower()
    {
        if (flowerPrefab == null)
            return;

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        if (
            Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out RaycastHit hit,
                0.5f
            )
        )
        {
            spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            spawnPosition = hit.point;
        }

        _spawnedFlower = Instantiate(flowerPrefab, spawnPosition, spawnRotation);
        flowerAnimator = _spawnedFlower.GetComponent<Animator>();

        gameObject.SetActive(false);
    }

    private IEnumerator GrowRoutine()
    {
        float duration = 2f;
        float elapsed = 0f;
        transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // EaseOut pour un effet naturel
            transform.localScale = Vector3.Lerp(
                Vector3.zero,
                _initialScale,
                Mathf.SmoothStep(0f, 1f, t)
            );
            yield return null;
        }

        transform.localScale = _initialScale;
    }

    private void ConfigureRigidbody()
    {
        _rb.mass = 0.01f; // très léger
        _rb.linearDamping = 3f; // résistance de l'air
        _rb.angularDamping = 0.5f; // résistance à la rotation
    }
}
