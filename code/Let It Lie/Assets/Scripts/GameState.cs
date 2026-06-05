using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameState", menuName = "Game/GameState")]
public class GameState : ScriptableObject
{
    [Header("Global state")]
    public float life = 0f;

    public event Action<float> OnRecoverLife;
    public event Action<float> OnDecreaseLife;

    public void Reset()
    {
        life = 0f;
    }

    public void RecoverLife(float amount)
    {
        life += amount;
        if (life > 100f)
            life = 100f;

        OnRecoverLife?.Invoke(amount);
    }

    public void DecreaseLife(float amount)
    {
        life -= amount;
        if (life < 0f)
            life = 0f;

        OnDecreaseLife?.Invoke(amount);
    }
}
