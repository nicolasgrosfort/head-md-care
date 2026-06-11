using UnityEngine;

public class FlowerZone : MonoBehaviour
{
    private Renderer _renderer;

    private void Awake() => _renderer = GetComponent<Renderer>();

    public Vector3 GetRandomPosition()
    {
        Bounds b = _renderer.bounds;
        return new Vector3(Random.Range(b.min.x, b.max.x), b.min.y, Random.Range(b.min.z, b.max.z));
    }
}
