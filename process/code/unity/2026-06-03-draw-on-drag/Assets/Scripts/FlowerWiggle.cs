using UnityEngine;

public class FlowerWiggle : MonoBehaviour
{
    public float wiggleStrength = 15f;
    public float wiggleSpeed = 2f;
    public float damping = 3f;
    public float spawnBurstStrength = 30f;

    private Transform stemBone;
    private Quaternion baseRotation;
    private float burst;

    void Start()
    {
        // Cherche le bone par nom dans tous les enfants
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            // Adapte "Bone" au nom exact de ton bone de tige dans Blender
            if (child.name.Contains("Bone") || child.name.Contains("Stem"))
            {
                stemBone = child;
                // Debug.Log("Bone trouvé : " + stemBone.name);
                break;
            }
        }

        if (stemBone == null)
        {
            // Fallback : prend le premier enfant
            stemBone = transform.GetChild(0);
            // Debug.Log("Fallback bone : " + stemBone.name);
        }

        baseRotation = stemBone.localRotation;
        burst = spawnBurstStrength;
    }

    void Update()
    {
        if (stemBone == null)
            return;

        burst = Mathf.Lerp(burst, 0f, Time.deltaTime * 3f);
        float x = Mathf.Sin(Time.time * wiggleSpeed) * (wiggleStrength + burst);
        float z = Mathf.Cos(Time.time * wiggleSpeed * 0.7f) * (wiggleStrength + burst);

        Quaternion target = baseRotation * Quaternion.Euler(x, 0, z);
        stemBone.localRotation = Quaternion.Lerp(
            stemBone.localRotation,
            target,
            Time.deltaTime * damping
        );
    }
}
