using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float fadeDuration = 1f;

    [SerializeField]
    private string nextScene;

    void Start()
    {
        animator.SetTrigger("FadeIn");
    }

    public void GoToNextScene()
    {
        StartCoroutine(WaitAndLoad());
    }

    private IEnumerator WaitAndLoad()
    {
        animator.SetTrigger("FadeOut");
        yield return new WaitForSeconds(fadeDuration);
        SceneManager.LoadScene(nextScene);
    }
}
