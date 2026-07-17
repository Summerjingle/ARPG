using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightRoomTrigger : MonoBehaviour
{
    public GameObject door;
    public Animator doorAnim;
    public Animator BossBloodBarAnim;
    public AudioClip doorSound;
    public GameObject enemyHealthBar;
    private bool isplayerintheroom=false;
    


   

  
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isplayerintheroom){
            isplayerintheroom = true;
            doorAnim.SetTrigger("Close");
            
            enemyHealthBar.SetActive(true);
            AudioManager.Instance.PlaySFX(doorSound,transform.position);
            BossBloodBarAnim.SetTrigger("ShowBar");
            Debug.Log("��ҽ��뷿��");
        }
    }
}
