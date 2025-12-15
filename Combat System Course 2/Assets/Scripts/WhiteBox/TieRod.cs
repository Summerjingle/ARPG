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
    public GameObject fire_R;
    [Header("Camera Zoom")]
    public float normalFov = 40f;
    public float zoomFov = 20f;
    public float zoomDuration = 0.6f;


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
        snakePillar_R_Anim.SetTrigger("Turn");
        stoneSound.Play();
        yield return new WaitForSeconds(8.0f);
        StartCoroutine(SmoothZoom(normalFov, zoomFov, zoomDuration));
        yield return new WaitForSeconds(3.0f);
        closeUpCam.Priority = 10;
    }
    IEnumerator SmoothZoom(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            closeUpCam.m_Lens.FieldOfView = Mathf.Lerp(from, to, t);
            yield return null;
        }
        closeUpCam.m_Lens.FieldOfView = to;
        fire_R.SetActive(true);
    }
}
