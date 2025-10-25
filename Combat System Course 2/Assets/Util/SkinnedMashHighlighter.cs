using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedMashHighlighter : MonoBehaviour
{
    [SerializeField] List<SkinnedMeshRenderer> meshesToHighLight;
    [SerializeField] Material originMaterial;
    [SerializeField] Material highlightedMaterial;

    public void HighlightMesh(bool highlight)
    {
        foreach (var mesh in meshesToHighLight)
        {
            mesh.material = (highlight) ? highlightedMaterial : originMaterial;
        }
    }
}
