using System;
using UnityEngine;

public class RootManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField]
    private GameState gameState;

    [Serializable]
    private class RootBone
    {
        public Transform bone;
        public float minScale;
        public float maxScale;
    }

    [SerializeField]
    private RootBone[] rootBones;

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
        foreach (var rootBone in rootBones)
        {
            if (rootBone.bone != null)
            {
                float scale = Mathf.Lerp(rootBone.minScale, rootBone.maxScale, gameState.life);
                rootBone.bone.localScale = new Vector3(scale, scale, scale);
            }
        }
    }
}
