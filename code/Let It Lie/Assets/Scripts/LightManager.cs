using UnityEngine;

public class LightManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;
    private Light directionalLight;

    public Color nightColor = new Color(0.05f, 0.05f, 0.15f);
    public Color dayColor = new Color(0.8f, 0.4f, 0.2f);

    public float nightIntensity = 0.2f;
    public float dayIntensity = 1f;

    void Start()
    {
        directionalLight = GetComponent<Light>();
    }

    void Update()
    {
        float t = Mathf.Sin(gameState.time * Mathf.PI);
        directionalLight.color = Color.Lerp(nightColor, dayColor, t);
        directionalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
    }
}
