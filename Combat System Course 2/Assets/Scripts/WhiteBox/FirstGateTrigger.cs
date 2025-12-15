using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstGateTrigger : MonoBehaviour
{
    private Animator firstGateAnim;
    private bool isInTrigger = false;
    public GameObject lockedInfo;
    void Start()
    {
        firstGateAnim = GetComponentInParent<Animator>();
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
            firstGateAnim.SetTrigger("GateOpen");
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
