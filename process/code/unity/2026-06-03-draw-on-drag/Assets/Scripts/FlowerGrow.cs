using UnityEngine;

public class FlowerGrow : MonoBehaviour
{
    public float growDuration = 0.8f;
    public float minScale = 1f;
    public float maxScale = 3f;
    public AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float timer = 0f;
    private float targetScale;

    void Start()
    {
        // Choisit une taille aléatoire au spawn
        targetScale = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (timer < growDuration)
        {
            timer += Time.deltaTime;
            float t = growCurve.Evaluate(timer / growDuration);
            transform.localScale = Vector3.one * (t * targetScale);
        }
        else
        {
            if (!GetComponent<FlowerWiggle>())
                gameObject.AddComponent<FlowerWiggle>();

            Destroy(this);
        }
    }
}
