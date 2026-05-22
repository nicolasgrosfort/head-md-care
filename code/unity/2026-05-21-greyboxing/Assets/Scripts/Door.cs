using UnityEngine;

public class Door : MonoBehaviour
{
    private void OnMouseDown()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("0-Initial");
    }
}
