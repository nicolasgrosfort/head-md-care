using NUnit.Framework.Constraints;
using UnityEngine;

public class RootManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField]
    private GameState gameState;

    private Transform _mainRoot;
    private Transform _upRoot;
    private Transform _downRoot;

    void Awake()
    {
        _mainRoot = FindBoneRecursive(transform, "MainRoot");
        _upRoot = FindBoneRecursive(transform, "UpRoot");
        _downRoot = FindBoneRecursive(transform, "DownRoot");
    }

    void OnEnable()
    {
        gameState.OnLifeChange += HandleRootsSize;
    }

    void OnDisable()
    {
        gameState.OnLifeChange -= HandleRootsSize;
    }

    private void HandleRootsSize(float lifeChange)
    {
        float mainScale = Mathf.Lerp(0.8f, 1f, gameState.life);
        float upScale = Mathf.Lerp(0.1f, 1f, gameState.life);
        float downScale = Mathf.Lerp(0.1f, 1f, gameState.life);

        if (_mainRoot != null)
            _mainRoot.localScale = new Vector3(mainScale, mainScale, mainScale);
        if (_upRoot != null)
            _upRoot.localScale = new Vector3(upScale, upScale, upScale);
        if (_downRoot != null)
            _downRoot.localScale = new Vector3(downScale, downScale, downScale);
    }

    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.name == boneName)
                return child;
        }
        return null;
    }
}
