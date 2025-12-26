using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelInfo : MonoBehaviour
{
    public Bounds modelBounds;

    private void Awake()
    {
        CalculateBounds();
    }

    public void CalculateBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        modelBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            modelBounds.Encapsulate(renderers[i].bounds);
        }
    }

    void OnDrawGizmos()
    {
        CalculateBounds();
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(modelBounds.center, modelBounds.size);
    }
}
