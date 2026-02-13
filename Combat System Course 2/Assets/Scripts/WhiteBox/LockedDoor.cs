using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    private bool isInTrigger=false;
    private PlayerInputActions inputActions;

  
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")&& !isInTrigger)
        {
                isInTrigger = true;
                Debug.Log("Player entered the locked door");
                
        }
    }
    

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInTrigger = false;
        }
    }
}





