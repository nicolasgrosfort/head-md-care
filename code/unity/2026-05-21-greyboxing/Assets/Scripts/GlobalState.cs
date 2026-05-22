using UnityEngine;

public class GlobalState
{
    private static int _natureHealth = 100;

    public static int NatureHealth
    {
        get { return _natureHealth; }
        set
        {
            _natureHealth = value;
            OnNatureHealthChanged?.Invoke(_natureHealth);
        }
    }

    // Événement pour notifier les changements
    public static event System.Action<int> OnNatureHealthChanged;
}
