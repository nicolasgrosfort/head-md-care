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

    public float lifeIncrement = 0.00001f;
    public float lifeDecrement = -0.0001f;

    [Header("Season")]
    [Tooltip("0 = spring, 0.25 = summer, 0.5 = fall, 0.75 = winter")]
    [Range(0f, 1f)]
    public float season = 0f;
    private readonly float seasonCycle = 0.25f;
    private int currentSeason = -1;

    [Range(0f, 1f)]
    public float defaultSeason = 0.25f;

    [Header("Time")]
    [Tooltip("0 = night, 0.25 = sunrise, 0.5 = day, 0.75 = sunset")]
    [Range(0f, 1f)]
    public float time = 0f;

    [Range(0f, 1f)]
    public float defaultTime = 0f;

    [Range(0.0001f, 0.1f)]
    public float timeFactor = 0.001f;
    private int currentDayNight = -1;

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
    public float timeSpeedIncrement = 0.2f;

    [Range(1f, 10f)]
    public float timeScale = 10f;

    [Header("Wind")]
    public Vector3 windForce = new Vector3(0.5f, 0f, 0f);
    public float windTurbulence = 0.2f;

    public enum InteractionType
    {
        None,
        Drag,
        Click,
        Hold,
    }

    private InteractionType currentInteraction = InteractionType.None;

    public event Action<float> OnLifeChange;
    public event Action<float> OnTimeChange;
    public event Action<int, int> OnDayNightChange;
    public event Action<int> OnSeasonChange;
    public event Action<float> OnSpeedChange;
    public event Action OnClick;
    public event Action OnHold;
    public event Action OnDrag;
    public event Action OnInteractionEnd;
    public event Action<InteractionType> OnInteractionChange;
    public event Action<int, int> OnSpringNight;
    public event Action<int, int> OnSummerNight;
    public event Action<int, int> OnFallNight;
    public event Action<int, int> OnWinterNight;
    public event Action<int, int> OnSpringDay;
    public event Action<int, int> OnSummerDay;
    public event Action<int, int> OnFallDay;
    public event Action<int, int> OnWinterDay;

    /* PUBLIC COMPUTED PROPERTIES */
    public float NormalisedTimeSpeed =>
        Mathf.InverseLerp(defaultTimeSpeed, maxTimeSpeed, timeSpeed);

    public float LifeChange => Mathf.Lerp(lifeIncrement, lifeDecrement, NormalisedTimeSpeed);

    public int CurrentSeason => currentSeason;
    public int CurrentDayNight => currentDayNight;
    public InteractionType CurrentInteraction => currentInteraction;

    public float Spring => 0.25f;
    public float Summer => 0.5f;
    public float Fall => 0.75f;
    public float Winter => 1f;
    public float Day => 0.5f;
    public float Night => 0f;

    /* PUBLIC METHODS */
    public void OnEnable() => Reset();

    public void OnDisable()
    {
        OnLifeChange = null;
        OnTimeChange = null;
        OnSeasonChange = null;
        OnSpeedChange = null;
        OnDayNightChange = null;
        OnInteractionChange = null;
        OnClick = null;
        OnHold = null;
        OnDrag = null;
        OnInteractionEnd = null;
        Reset();
    }

    public void SetInteraction(InteractionType type)
    {
        currentInteraction = type;
        OnInteractionChange?.Invoke(currentInteraction);

        switch (type)
        {
            case InteractionType.Click:
                OnClick?.Invoke();
                break;
            case InteractionType.Hold:
                OnHold?.Invoke();
                break;
            case InteractionType.Drag:
                OnDrag?.Invoke();
                break;
            case InteractionType.None:
                OnInteractionEnd?.Invoke();
                break;
        }
    }

    public void Reset()
    {
        life = defaultLife;
        season = defaultSeason;
        time = defaultTime;
        timeSpeed = defaultTimeSpeed;
        currentSeason = Mathf.FloorToInt(defaultSeason / seasonCycle) % 4;
        currentDayNight = Mathf.FloorToInt(defaultTime / 0.5f);
    }

    public void IncreaseTime()
    {
        time += timeSpeed * timeFactor;
        if (time > 1f)
            time = 0f;

        ChangeSeason();
        ChangeDayNight();
        ChangeLife();

        OnTimeChange?.Invoke(time);
    }

    public void IncreaseTimeSpeed()
    {
        float t = (timeSpeed - defaultTimeSpeed) / (maxTimeSpeed - defaultTimeSpeed);
        float increment = timeSpeedIncrement * 0.03f * (2f * t + 0.2f);
        timeSpeed = Mathf.Min(timeSpeed + increment, maxTimeSpeed);
        OnSpeedChange?.Invoke(timeSpeed);
    }

    public void DecreaseTimeSpeed()
    {
        float t = (timeSpeed - defaultTimeSpeed) / (maxTimeSpeed - defaultTimeSpeed);
        float increment = timeSpeedIncrement * 0.04f * (2f * t + 0.05f);
        timeSpeed = Mathf.Max(timeSpeed - increment, minTimeSpeed);
        OnSpeedChange?.Invoke(timeSpeed);
    }

    /* PRIVATE METHODS */

    private void ChangeLife()
    {
        life += LifeChange;
        if (life > 1f)
            life = 1f;
        else if (life < 0f)
            life = 0f;

        OnLifeChange?.Invoke(LifeChange);
    }

    private void ChangeSeason()
    {
        season += timeSpeed * timeFactor * seasonCycle;
        if (season >= 1f)
            season = 0f;
    }

    private void ChangeDayNight()
    {
        int dayNight = Mathf.FloorToInt(time / 0.5f);
        if (currentDayNight == dayNight)
            return;

        currentDayNight = dayNight;

        if (currentDayNight == 0)
        {
            int nextSeason = Mathf.FloorToInt(season / seasonCycle);
            if (currentSeason != nextSeason)
            {
                currentSeason = nextSeason;
                OnSeasonChange?.Invoke(currentSeason);
            }
        }

        OnDayNightChange?.Invoke(currentDayNight, currentSeason);
        DispatchDaySeasonEvent(currentDayNight, currentSeason);
    }

    private void DispatchDaySeasonEvent(int cycle, int season)
    {
        Action<int, int> evt = (cycle, season) switch
        {
            (0, 0) => OnSpringNight,
            (0, 1) => OnSummerNight,
            (0, 2) => OnFallNight,
            (0, 3) => OnWinterNight,
            (1, 0) => OnSpringDay,
            (1, 1) => OnSummerDay,
            (1, 2) => OnFallDay,
            (1, 3) => OnWinterDay,
            _ => null,
        };
        evt?.Invoke(cycle, season);

        Debug.Log($"Season: {season}, Cycle: {cycle}");
    }
}
