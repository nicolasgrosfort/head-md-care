using UnityEngine;

public class MoonManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;
    public float radius = 10f;
    public float initialAngle = 0f; // Angle de départ en degrés

    void Update()
    {
        float angle = gameState.time * 360f + initialAngle;
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
        transform.position = new Vector3(x, y, transform.position.z);
    }
}
