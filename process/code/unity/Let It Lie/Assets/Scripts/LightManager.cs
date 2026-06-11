using UnityEngine;

public class LightManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;
    private Light directionalLight;

    [Header("Lumière directionnelle")]
    public Color nightColor = new Color(0.05f, 0.05f, 0.15f);
    public Color dayColor = new Color(0.8f, 0.4f, 0.2f);
    public float nightIntensity = 0.2f;
    public float dayIntensity = 1f;

    [Header("Ambient par saison")]
    public Color springAmbient = new Color(0.3f, 0.3f, 0.4f);
    public Color summerAmbient = new Color(0.2f, 0.2f, 0.3f);
    public Color fallAmbient = new Color(0.4f, 0.3f, 0.2f);
    public Color winterAmbient = new Color(0.1f, 0.1f, 0.2f);

    void Start()
    {
        directionalLight = GetComponent<Light>();
    }

    void Update()
    {
        // Lumière jour/nuit
        float t = Mathf.Sin(gameState.time * Mathf.PI);
        directionalLight.color = Color.Lerp(nightColor, dayColor, t);
        directionalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);

        // Ambient avec lerp entre saisons
        RenderSettings.ambientLight = GetSeasonAmbient(gameState.season);
    }

    Color GetSeasonAmbient(float season)
    {
        if (season < 0.25f)
            return Color.Lerp(springAmbient, summerAmbient, season / 0.25f);
        else if (season < 0.5f)
            return Color.Lerp(summerAmbient, fallAmbient, (season - 0.25f) / 0.25f);
        else if (season < 0.75f)
            return Color.Lerp(fallAmbient, winterAmbient, (season - 0.5f) / 0.25f);
        else
            return Color.Lerp(winterAmbient, springAmbient, (season - 0.75f) / 0.25f);
    }
}
