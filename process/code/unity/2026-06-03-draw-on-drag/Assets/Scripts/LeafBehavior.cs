using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LeafBehaviour : MonoBehaviour
{
    [Range(0.1f, 5f)]
    public float drag = 2.5f;

    [Range(0f, 1f)]
    public float angularDrag = 0.8f;

    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Init()
    {
        _rb.WakeUp();
        _rb.linearDamping = drag + Random.Range(-0.5f, 0.5f);
        _rb.angularDamping = angularDrag;
        _rb.mass = 0.01f;

        // Légère impulsion latérale pour qu'elles dérivent
        _rb.AddForce(
            new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f)),
            ForceMode.Impulse
        );

        _rb.AddTorque(Random.insideUnitSphere * 0.3f, ForceMode.Impulse);
    }
}
