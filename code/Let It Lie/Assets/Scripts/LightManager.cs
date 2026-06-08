using UnityEngine;

public class LightManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;

    public Color nightColor = new Color(0.05f, 0.05f, 0.15f); // 0
    public Color dayColor = new Color(0.8f, 0.4f, 0.2f); // 1

    public float nightIntensity = 0.2f;
    public float dayIntensity = 1f;

    void Start() { }

    void Update()
    {
        float t = gameState.time;
        Color color = Color.Lerp(nightColor, dayColor, t);
        float intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);

        RenderSettings.ambientLight = color;
        RenderSettings.ambientIntensity = intensity;
    }
}
