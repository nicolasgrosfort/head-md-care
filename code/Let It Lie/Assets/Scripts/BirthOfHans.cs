using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class BirthOfHans : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private GameState gameState;

    [SerializeField]
    private readonly float recovery = 0.1f;

    [SerializeField]
    private readonly float minDuration = 0.6f;

    [SerializeField]
    private readonly float maxDuration = 2.4f;

    [SerializeField]
    private Color fullLifeColor = Color.white;

    [SerializeField]
    private Color emptyLifeColor = Color.black;

    [SerializeField]
    private GameObject bulb;

    [SerializeField]
    private GameObject petals;

    [SerializeField]
    private ParticleSystem particles;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private SceneTransition sceneTransition;

    private Coroutine holdCoroutine;

    void OnEnable()
    {
        gameState.OnRecoverLife += UpdatePosition;
        gameState.OnDecreaseLife += UpdatePosition;
    }

    void OnDisable()
    {
        gameState.OnRecoverLife -= UpdatePosition;
        gameState.OnDecreaseLife -= UpdatePosition;
    }

    void Start()
    {
        UpdatePosition(gameState.life);
    }

    void UpdatePosition(float currentLife)
    {
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            transform.localPosition.y + currentLife * 0.05f,
            transform.localPosition.z
        );

        bulb.SetActive(gameState.life >= 100f);
        petals.SetActive(gameState.life >= 100f);

        float t = gameState.life / 100f;
        mainCamera.backgroundColor = Color.Lerp(emptyLifeColor, fullLifeColor, t);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        holdCoroutine = StartCoroutine(HoldRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private IEnumerator HoldRoutine()
    {
        if (gameState.life >= 100f)
        {
            sceneTransition.GoToNextScene();
            yield break;
        }

        float duration = Random.Range(minDuration, maxDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float curve = Mathf.Sin(t * Mathf.PI);

            var main = particles.main;
            main.simulationSpeed = 1f + curve * 4f;
            main.startSize = 0.01f + curve * 0.01f;

            gameState.RecoverLife(recovery * curve);
            elapsed += Time.deltaTime;
            yield return null;
        }

        var mainReset = particles.main;
        mainReset.simulationSpeed = 1f;
        mainReset.startSize = 0.01f;
    }
}
