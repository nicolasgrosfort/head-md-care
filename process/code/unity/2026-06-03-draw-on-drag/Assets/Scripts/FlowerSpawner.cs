using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FlowerSpawner : MonoBehaviour
{
    public GameObject flowerPrefab;
    public Camera cam;

    void Update()
    {
        if (
            Touchscreen.current != null
            && Touchscreen.current.primaryTouch.press.wasPressedThisFrame
        )
        {
            Debug.Log("Touch détecté !");
            // ...
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Clic souris détecté !");
            Vector2 mousePos = Mouse.current.position.ReadValue();
            TrySpawn(mousePos);
        }
    }

    void TrySpawn(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        Debug.Log("Raycast lancé depuis : " + screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Hit : " + hit.collider.name + " à " + hit.point);

            // Bloque si le layer est "Obstacle"
            if (
                hit.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle")
                || hit.collider.gameObject.layer == LayerMask.NameToLayer("Leaf")
                || hit.collider.gameObject.layer == LayerMask.NameToLayer("Ivy")
            )
            {
                Debug.Log("Obstacle détecté, pas de fleur ici !");
                return;
            }

            SpawnFlower(hit.point, hit.normal);
        }
        else
        {
            Debug.Log("Rien touché - pas de collider ?");
        }
    }

    void SpawnFlower(Vector3 position, Vector3 normal)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        GameObject flower = Instantiate(flowerPrefab, position, rotation);
        flower.AddComponent<FlowerGrow>();
    }
}
