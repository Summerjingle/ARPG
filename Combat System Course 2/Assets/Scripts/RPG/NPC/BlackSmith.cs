using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackSmith : MonoBehaviour
{
    public GameObject tips;
    public GameObject blacksmithDialogue;
    private bool isInTrigger=false;
    public CinemachineVirtualCamera blacksmithCam;

    private void Awake()
    {
        tips.SetActive(false);
        hideDialogue();
    }
    private void OnTriggerEnter(Collider other)
    {
        if(isInTrigger == false && other.CompareTag("Player") )
        {
            isInTrigger = true;
            tips.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInTrigger = false;
            tips.SetActive(false);
        }
    }
    private void Update()
    {
        if (isInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            tips.SetActive(false);
            blacksmithDialogue.SetActive(true);
            UIStateManager.SetUIActive(true);
            blacksmithCam.Priority = 30;
        }
    }

    
    public void hideDialogue()
    {
        blacksmithDialogue.SetActive(false);
        UIStateManager.SetUIActive(false);
        blacksmithCam.Priority = 10;
    }
}
