using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WarningTrigger : MonoBehaviour
{

    public AudioClip warningSentence;
    private AudioSource audioSource;
    private BoxCollider boxCollider;


    void Start()
    {
        audioSource=GetComponent<AudioSource>();
        boxCollider=GetComponent<BoxCollider>();
        AudioManager.RouteToSFX(audioSource);
    }

    // Update is called once per frame
    
        private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player"))
        {
            AudioManager.Instance.PlaySFX(warningSentence, transform.position);
            boxCollider.enabled=false;
        }
        }
    
}
