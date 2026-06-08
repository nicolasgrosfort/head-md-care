using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameState", menuName = "Game/GameState")]
public class GameState : ScriptableObject
{
    [Header("Global state")]
    public float life = 0f; // 0-1, 0 = death, 1 = alive
    public float season = 0f; // 0-1, 0 = spring, 0.25 = summer, 0.5 = autumn, 0.75 = winter
    public float time = 0f; // 0-1, 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset
    public float timeSpeed = 0.00001f; // how fast time progresses
    public float timeSpeedIncrement = 0.000000001f;
    public float defaultTimeSpeed = 0.00001f;
    public float maxTimeSpeed = 0.0001f;

    public event Action<float> OnRecoverLife;
    public event Action<float> OnDecreaseLife;

    public void OnEnable() => Reset();

    public void OnDisable() => Reset();

    public void Reset()
    {
        life = 0f;
        season = 0f;
        time = 0f;
        timeSpeed = defaultTimeSpeed;
    }

    public void IncreaseTime()
    {
        time += timeSpeed;

        if (time > 1f)
        {
            time = 0f;
            IncreaseSeason();
        }
    }

    public void IncreaseSeason(float amount = 0.25f)
    {
        season += amount;
        if (season >= 1f)
            season = 0f;
    }

    public void IncreaseTimeSpeed(float amount = 0.000001f) =>
        timeSpeed = Mathf.Min(timeSpeed + amount, maxTimeSpeed);

    public void DecreaseTimeSpeed(float amount = 0.000001f) =>
        timeSpeed = Mathf.Max(timeSpeed - amount, defaultTimeSpeed);

    public void ResetTimeSpeed() => timeSpeed = defaultTimeSpeed;

    public void RecoverLife(float amount)
    {
        life += amount;
        if (life > 1f)
            life = 1f;

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
