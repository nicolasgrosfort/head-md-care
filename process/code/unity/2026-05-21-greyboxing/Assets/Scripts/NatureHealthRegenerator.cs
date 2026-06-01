using System.Collections;
using UnityEngine;

public class NatureHealthRegenerator : MonoBehaviour
{
    private const int MaxNatureHealth = 100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject root = new GameObject(nameof(NatureHealthRegenerator));
        DontDestroyOnLoad(root);
        root.AddComponent<NatureHealthRegenerator>();
    }

    private void Start()
    {
        StartCoroutine(RegenerateNatureHealth());
    }

    private IEnumerator RegenerateNatureHealth()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);

        while (true)
        {
            yield return wait;

            if (GlobalState.NatureHealth < MaxNatureHealth)
            {
                GlobalState.NatureHealth = Mathf.Min(GlobalState.NatureHealth + 2, MaxNatureHealth);
            }
        }
    }
}
