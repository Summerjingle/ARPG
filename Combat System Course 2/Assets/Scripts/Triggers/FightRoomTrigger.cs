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
    public Animator caveCamera;


   

  
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isplayerintheroom){
            isplayerintheroom = true;
            doorAnim.SetTrigger("Close");
            caveCamera.SetTrigger("MeetBoss");
            enemyHealthBar.SetActive(true);
            AudioSource.PlayClipAtPoint(doorSound,transform.position);
            BossBloodBarAnim.SetTrigger("ShowBar");
            Debug.Log("玩家进入房间");
        }
    }
}
