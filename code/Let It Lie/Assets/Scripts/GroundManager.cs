using UnityEngine;

public class GroundManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;

    private float initial = 20f;
    public float min = -10;
    public float max = 20f;

    void Start()
    {
        initial = transform.position.y;
    }

    void Update()
    {
        float progressiveSeason = gameState.season + gameState.time * 0.25f;
        // float t = (Mathf.Sin(progressiveSeason * Mathf.PI * 2f) + 1f) * 0.5f;
        // float y = initial + Mathf.Lerp(min, max, t);
        // transform.position = new Vector3(transform.position.x, y, transform.position.z);

        float t = Mathf.Sin((progressiveSeason - 0.25f) * Mathf.PI * 2f);
        float y = initial + Mathf.Lerp(0f, t > 0f ? max : min, Mathf.Abs(t));
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }
}
