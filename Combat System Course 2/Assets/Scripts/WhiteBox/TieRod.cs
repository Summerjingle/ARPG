using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class TieRod : MonoBehaviour
{
    public AudioSource stoneSound;
    public AudioSource fireUpSound;
    private Animator tieRod;
    public Animator snakePillar_R_Anim;
    private bool isInTrigger = false;
    private bool isActivated = false;
    public CinemachineVirtualCamera closeUpCam;
    public CinemachineVirtualCamera closeUpCam2;
    public GameObject fire_R;
    


    void Start()
    {
        tieRod = GetComponentInParent<Animator>();
        fire_R.SetActive(false);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!isActivated&&!isInTrigger&& other.CompareTag("Player"))
        {
            isInTrigger = true;
        }
    }
    private void Update()
    {
        if (!isActivated && isInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            
            tieRod.SetTrigger("Activate");
            isActivated = true;
            StartCoroutine(PillarTurn());
            gameObject.GetComponent<CapsuleCollider>().enabled = false;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInTrigger = false;
        }
    }

    IEnumerator PillarTurn()
    {
        yield return new WaitForSeconds(1.0f);
        closeUpCam.Priority = 30;
        closeUpCam2.Priority = 25;
        snakePillar_R_Anim.SetTrigger("Turn");
        stoneSound.Play();
        yield return new WaitForSeconds(7.0f);
        StartCoroutine(FireUp());
    }
    IEnumerator FireUp()
    {
        closeUpCam.Priority = 10;
        yield return new WaitForSeconds(1.5f);
        fire_R.SetActive(true);
        fireUpSound.Play();
        yield return new WaitForSeconds(3f);
        closeUpCam2.Priority = 10;
    }
}
