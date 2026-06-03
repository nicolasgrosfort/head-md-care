using TMPro;
using UnityEngine;

public class MossCounter : MonoBehaviour
{
    public static MossCounter Instance { get; private set; }

    [Header("UI")]
    public TMP_Text counterText;

    public float Percentage => Total > 0 ? (float)Remaining / Total * 100f : 0f;

    public int Total { get; private set; }
    public int Remaining { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void Register(int count)
    {
        Total += count;
        Remaining += count;
        UpdateUI();
    }

    public void Remove(int count = 1)
    {
        Remaining = Mathf.Max(0, Remaining - count);
        UpdateUI();
    }

    public void Reset()
    {
        Total = 0;
        Remaining = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        int percentage = Total > 0 ? Mathf.RoundToInt((float)Remaining / Total * 100f) : 0;

        if (counterText != null)
            counterText.text = $"Mousse : {percentage}%";

        Debug.Log($"Mousse : {percentage}% ({Remaining} / {Total})");
    }
}
