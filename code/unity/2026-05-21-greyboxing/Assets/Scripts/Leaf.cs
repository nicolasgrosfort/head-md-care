using UnityEngine;

public class Leaf : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnMouseDown()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("4-Space");
    }
}
