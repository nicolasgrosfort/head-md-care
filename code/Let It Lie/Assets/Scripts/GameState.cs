using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameState", menuName = "Game/GameState")]
public class GameState : ScriptableObject
{
    [Header("Global state")]
    [Range(0f, 1f)]
    public float life = 0.5f; // 0-1, 0 = not enough, 0.5 = balanced, 1 = too much

    [Range(0f, 1f)]
    public float season = 0f; // 0-1, 0 = spring, 0.25 = summer, 0.5 = autumn, 0.75 = winter

    [Header("Time")]
    [Range(0f, 1f)]
    public float time = 0f; // 0-1, 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset

    [Range(0f, 1f)]
    public float timeSpeed = 0.01f;

    [Range(0f, 1f)]
    public float defaultTimeSpeed = 0.01f;

    [Range(0f, 1f)]
    public float maxTimeSpeed = 1f;

    [Range(0f, 1f)]
    public float minTimeSpeed = 0f;

    [Range(0f, 1f)]
    public float timeSpeedIncrement = 0.1f;

    public event Action<float> OnRecoverLife;
    public event Action<float> OnDecreaseLife;

    public void OnEnable() => Reset();

    public void OnDisable() => Reset();

    public void Reset()
    {
        life = 0.5f;
        season = 0f;
        time = 0f;
        timeSpeed = defaultTimeSpeed;
    }

    public void IncreaseTime()
    {
        time += timeSpeed * 0.001f;

        if (time > 1f)
        {
            time = 0f;
            IncreaseSeason();
        }
    }

    public void IncreaseSeason()
    {
        float seasonIncrement = 0.25f;
        season += seasonIncrement;

        if (season >= 1f)
            season = 0f;
    }

    public void IncreaseTimeSpeed()
    {
        float t = (timeSpeed - defaultTimeSpeed) / (maxTimeSpeed - defaultTimeSpeed);
        float increment = timeSpeedIncrement * 0.01f * (2f * t + 0.05f);
        timeSpeed = Mathf.Min(timeSpeed + increment, maxTimeSpeed);
    }

    public void DecreaseTimeSpeed()
    {
        float t = (timeSpeed - defaultTimeSpeed) / (maxTimeSpeed - defaultTimeSpeed);
        float increment = timeSpeedIncrement * 0.02f * (2f * t + 0.05f);
        timeSpeed = Mathf.Max(timeSpeed - increment, minTimeSpeed);
    }

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
