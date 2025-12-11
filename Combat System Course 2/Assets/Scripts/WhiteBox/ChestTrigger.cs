using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestTrigger : MonoBehaviour
{
    private bool isInTrigger=false;
    private bool isCollected=false;
    public Material defaultMaterial;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!isCollected&& other.CompareTag("Player"))
        {
            isInTrigger = true;
        }
    }
    private void Update()
    {
        if (!isCollected&& isInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = defaultMaterial;
            else
                Debug.LogWarning("该物体材质为空");
            isCollected = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isCollected)
            {
                gameObject.GetComponent<CapsuleCollider>().isTrigger = false;
            }
            isInTrigger = false;
        }
    }
}
