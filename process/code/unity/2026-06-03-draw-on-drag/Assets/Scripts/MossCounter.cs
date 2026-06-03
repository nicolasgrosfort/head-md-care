using TMPro;
using UnityEngine;

public class MossCounter : MonoBehaviour
{
    public static MossCounter Instance { get; private set; }

    [Header("UI")]
    public TMP_Text counterText;

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
        if (counterText != null)
            counterText.text = $"Mousse : {Remaining} / {Total}";

        Debug.Log($"Mousse : {Remaining} / {Total}");
    }
}
