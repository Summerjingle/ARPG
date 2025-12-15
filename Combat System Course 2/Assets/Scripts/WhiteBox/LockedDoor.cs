using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    private bool isInTrigger=false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")&& !isInTrigger)
        {
                isInTrigger = true;
                Debug.Log("从里面被锁住了");
                MessageUI.Instance.Show("这扇门只能里面打开");
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





