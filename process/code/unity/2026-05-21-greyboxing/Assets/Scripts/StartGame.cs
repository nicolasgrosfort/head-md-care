using UnityEngine;

public class StartGame : MonoBehaviour
{
    private void OnMouseDown()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("1-Underworld");
    }
}
