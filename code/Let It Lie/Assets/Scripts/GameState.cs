using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameState", menuName = "Game/GameState")]
public class GameState : ScriptableObject
{
    [Header("Life")]
    [Tooltip("0 = dead, 0.5 = balanced, 1 = alive")]
    [Range(0f, 1f)]
    public float life = 0.5f;

    [Range(0f, 1f)]
    public float defaultLife = 0.5f;

    [Header("Season")]
    [Tooltip("0-1, 0 = spring, 0.25 = summer, 0.5 = fall, 0.75 = winter")]
    [Range(0f, 1f)]
    public float season = 0f;
    private readonly float seasonCycle = 0.25f;

    [Range(0f, 1f)]
    public float defaultSeason = 0.5f;

    [Header("Time")]
    [Tooltip("0 = night, 0.25 = sunrise, 0.5 = day, 0.75 = sunset")]
    [Range(0f, 1f)]
    public float time = 0f;

    [Range(0f, 1f)]
    public float defaultTime = 0f;

    [Range(0.0001f, 0.1f)]
    public float timeFactor = 0.001f;

    [Header("Speed")]
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
    public event Action<float> OnTimeChange;
    public event Action<float> OnSeasonChange;

    public void OnEnable() => Reset();

    public void OnDisable() => Reset();

    public void Reset()
    {
        life = defaultLife;
        season = defaultSeason;
        time = defaultTime;
        timeSpeed = defaultTimeSpeed;
    }

    public void IncreaseTime()
    {
        time += timeSpeed * timeFactor;
        if (time > 1f)
            time = 0f;

        OnTimeChange?.Invoke(time);
        IncreaseSeason();
    }

    public void IncreaseSeason()
    {
        season += timeSpeed * timeFactor * seasonCycle;
        if (season >= 1f)
            season = 0f;

        if (season % seasonCycle == 0f)
            OnSeasonChange?.Invoke(season);
    }

    public void IncreaseTimeSpeed()
    {
        float t = (timeSpeed - defaultTimeSpeed) / (maxTimeSpeed - defaultTimeSpeed);
        float increment = timeSpeedIncrement * 0.03f * (2f * t + 0.2f);
        timeSpeed = Mathf.Min(timeSpeed + increment, maxTimeSpeed);
    }

    public void DecreaseTimeSpeed()
    {
        float t = (timeSpeed - defaultTimeSpeed) / (maxTimeSpeed - defaultTimeSpeed);
        float increment = timeSpeedIncrement * 0.04f * (2f * t + 0.05f);
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
