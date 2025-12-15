using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A3_DoorLocked : MonoBehaviour
{
    private Animator A3_Door;
    private bool isInTrigger = false;

    void Start()
    {
        A3_Door = GetComponentInParent<Animator>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isInTrigger)
        {
            isInTrigger = true;
            
        }
    }
    private void Update()
    {
        if (isInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            A3_Door.SetTrigger("Locked");
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
