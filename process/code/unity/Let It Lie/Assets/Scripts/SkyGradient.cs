using UnityEngine;

public class SkyGradient : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;

    public Color nightColor = new Color(0.05f, 0.05f, 0.15f); // 0
    public Color sunriseColor = new Color(0.8f, 0.4f, 0.2f); // 0.33
    public Color dayColor = new Color(0.4f, 0.7f, 1f); // 0.66
    public Color sunsetColor = new Color(0.9f, 0.3f, 0.1f); // 1

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void Update()
    {
        cam.backgroundColor = GetSkyColor(gameState.time);
    }

    Color GetSkyColor(float t)
    {
        if (t < 0.25f)
            return Color.Lerp(nightColor, sunriseColor, t / 0.25f);
        else if (t < 0.5f)
            return Color.Lerp(sunriseColor, dayColor, (t - 0.25f) / 0.25f);
        else if (t < 0.75f)
            return Color.Lerp(dayColor, sunsetColor, (t - 0.5f) / 0.25f);
        else
            return Color.Lerp(sunsetColor, nightColor, (t - 0.75f) / 0.25f);
    }
}
