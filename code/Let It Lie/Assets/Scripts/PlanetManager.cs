using UnityEngine;

public class PlanetManager : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;
    public float initialAngle = 0f;

    void Update()
    {
        float angle = gameState.time * 360f + initialAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
