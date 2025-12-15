using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A3_DoorOpen : MonoBehaviour
{
    private Animator A3_Door;
    private bool isInTrigger = false;
    public GameObject lockedInfo;
    void Start()
    {
        A3_Door = GetComponentInParent<Animator>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInTrigger = true;
        }
    }
    private void Update()
    {
        if (isInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            A3_Door.SetTrigger("Open");
            Destroy(lockedInfo);
            gameObject.GetComponent<Collider>().enabled = false;
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
