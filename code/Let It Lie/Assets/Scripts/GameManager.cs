using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Global state")]
    public int life = 100;

    void Awake()
    {
        // A single instance persists between the scenes.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RecoverLife(int amount)
    {
        life += amount;
        if (life > 100)
            life = 100;
    }

    public void DecreaseLife(int amount)
    {
        life -= amount;
        if (life < 0)
            life = 0;
    }
}
