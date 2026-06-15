using UnityEngine;

public class StarManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField]
    private GameState gameState;

    private ParticleSystem.EmissionModule emission;

    private void Awake()
    {
        emission = GetComponent<ParticleSystem>().emission;
    }

    private void OnEnable()
    {
        gameState.OnTimeChange += HandleStars;
    }

    private void OnDisable()
    {
        gameState.OnTimeChange -= HandleStars;
    }

    private void HandleStars(float time)
    {
        float distanceToMidday = Mathf.Abs(time - 0.5f) * 2f;
        float alpha = Mathf.SmoothStep(0f, 1f, distanceToMidday);
        float rateOverTime = Mathf.Lerp(0f, 100f, alpha);
        emission.rateOverTime = rateOverTime;
    }
}
