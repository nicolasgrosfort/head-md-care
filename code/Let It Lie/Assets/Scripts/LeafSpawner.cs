using UnityEngine;

public class LeafSpawner : MonoBehaviour
{
    public GameObject leafPrefab;
    public int maxLeaves = 100;

    [SerializeField]
    private SceneTransition sceneTransition;

    private int count = 0;

    void Update()
    {
        if (count >= maxLeaves)
        {
            sceneTransition.GoToNextScene();
            return;
        }

        if (Random.value < 0.05f) // ~3 feuilles/sec
        {
            Vector3 pos =
                transform.position + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
            var leaf = Instantiate(leafPrefab, pos, Random.rotation);
            float s = Random.Range(0.5f, 1.5f);
            leaf.transform.localScale = leafPrefab.transform.localScale * s;
            count++;
        }
    }
}
