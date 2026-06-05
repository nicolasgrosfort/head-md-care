using UnityEngine;

public class LeafFall : MonoBehaviour
{
    float speed;
    float drift;
    float rot;

    void Start()
    {
        speed = Random.Range(1f, 3f);
        drift = Random.Range(-0.5f, 0.5f);
        rot = Random.Range(-90f, 90f);
    }

    void Update()
    {
        transform.position += new Vector3(drift, -speed, 0) * Time.deltaTime;
        transform.Rotate(rot * Time.deltaTime, 0, 0);

        if (transform.position.y < 0f)
            Destroy(gameObject);
    }
}
