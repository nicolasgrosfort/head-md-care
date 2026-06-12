using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    [SerializeField]
    private float windForce = 800f;

    [SerializeField]
    private float windDuration = 2f;

    [SerializeField]
    public Vector3 windDirection = Vector3.right;

    [SerializeField]
    public float windRadius = 3f;

    [SerializeField]
    public Transform windPlane;

    [SerializeField]
    public float maxDistance = 5f;

    public void TriggerGust(List<Rigidbody> rigidbodies, Transform plane, Vector3 clickPoint)
    {
        foreach (var rb in rigidbodies)
            if (rb != null && !rb.isKinematic)
                StartCoroutine(ApplyGust(rb, plane, clickPoint));
    }

    private IEnumerator ApplyGust(Rigidbody rb, Transform plane, Vector3 clickPoint)
    {
        float elapsed = 0f;
        while (elapsed < windDuration)
        {
            float signedDist = Vector3.Dot(rb.position - plane.position, plane.forward);

            if (signedDist > 0f)
            {
                Vector3 flatLeaf = new Vector3(rb.position.x, 0f, rb.position.z);
                Vector3 flatClick = new Vector3(clickPoint.x, 0f, clickPoint.z);
                float lateralDist = Vector3.Distance(flatLeaf, flatClick);

                float depthFalloff = Mathf.Clamp01(1f - (signedDist / maxDistance));
                float lateralFalloff = Mathf.Clamp01(1f - (lateralDist / windRadius));

                // Pas de condition sur lateralFalloff, juste un multiplicateur
                rb.AddForce(
                    windDirection * windForce * depthFalloff * lateralFalloff,
                    ForceMode.Acceleration
                );
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
