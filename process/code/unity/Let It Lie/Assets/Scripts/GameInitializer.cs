using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [SerializeField]
    private GameState gameState;

    void Awake()
    {
        gameState.Reset();
    }

    void OnApplicationQuit()
    {
        gameState.Reset();
    }
}
