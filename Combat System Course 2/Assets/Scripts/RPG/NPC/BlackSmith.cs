using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackSmith : MonoBehaviour
{
    public GameObject tips;
    public GameObject blacksmithDialogue;
    public CinemachineVirtualCamera blacksmithCam;
    public AudioClip wellcom;
    public AudioClip end;

    private bool isInTrigger = false;
    private Animator ratManAnim;

    private void Awake()
    {
        ratManAnim = GetComponent<Animator>();
        tips.SetActive(false);
        blacksmithDialogue.SetActive(false);
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
            ratManAnim.SetTrigger("Welcom");
            AudioManager.Instance.PlaySFX(wellcom, transform.position);
            Debug.Log("wellcom");
            tips.SetActive(false);
            blacksmithDialogue.SetActive(true);
            UIStateManager.SetUIActive(true);
            blacksmithCam.Priority = 30;
        }
    }

    
    public void hideDialogue()
    {
        AudioManager.Instance.PlaySFX(end, transform.position);
        blacksmithDialogue.SetActive(false);
        UIStateManager.SetUIActive(false);
        blacksmithCam.Priority = 10;
        ratManAnim.SetTrigger("End");
        Debug.Log("end");
    }
}
