using UnityEngine;

public class CatchLeaf : MonoBehaviour
{
    private Light leafLight;
    private Renderer leafRenderer;
    private Material leafMaterial;
    private float baseLightIntensity = 0f;
    private Color baseEmissionColor = Color.white;

    private void Awake()
    {
        leafLight = GetComponentInChildren<Light>(true);
        leafRenderer = GetComponentInChildren<Renderer>(true);

        if (leafLight != null)
        {
            baseLightIntensity = leafLight.intensity;
        }

        if (leafRenderer != null)
        {
            leafMaterial = leafRenderer.material;
            if (leafMaterial != null)
            {
                baseEmissionColor = leafMaterial.GetColor("_EmissionColor");
            }
        }
    }

    private void OnEnable()
    {
        GlobalState.OnNatureHealthChanged += HandleNatureHealthChanged;

        HandleNatureHealthChanged(GlobalState.NatureHealth);
    }

    private void OnDisable()
    {
        GlobalState.OnNatureHealthChanged -= HandleNatureHealthChanged;
    }

    private void OnMouseDown()
    {
        GlobalState.NatureHealth -= 10;
        gameObject.SetActive(false);
    }

    private void HandleNatureHealthChanged(int natureHealth)
    {
        float healthRatio = Mathf.Clamp01(natureHealth / 100f);

        if (leafLight != null)
        {
            leafLight.intensity = baseLightIntensity * healthRatio;
        }

        if (leafMaterial != null)
        {
            leafMaterial.SetColor("_EmissionColor", baseEmissionColor * healthRatio);
        }
    }
}
