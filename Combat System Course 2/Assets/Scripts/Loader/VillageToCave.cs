using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VillageToCave : MonoBehaviour
{
    public string gameSceneName = "01Scene_Cave";
    public string loadingSceneName = "LoadingScene";
    public GameObject eTips;
    public Animator playerAnim;
    private bool isInTrigger=false;
    private bool isTeleporting = false;
    public CinemachineVirtualCamera TeleportCam;
    public Animator teleportLight;



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")&&!isInTrigger)
        {
            isInTrigger = true;
            if(eTips!=null)
                eTips.SetActive(true);
            
        }
    }
   private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInTrigger=false;
            if (eTips != null)
                eTips.SetActive(false);
            
        }
    }
    private void Update()
    {
        if (isInTrigger && Input.GetKeyDown(KeyCode.E) && !isTeleporting)
        {
            if (eTips != null)
                eTips.SetActive(false);
            Vector3 targetPosition = new Vector3(25.908f, 4.8f, -60.088f);  // Ŀ��λ��
            Quaternion targetRotation = Quaternion.Euler(0, 128.346f, 0);  // Ŀ����ת
            TeleportCam.Priority = 30;
            playerAnim.gameObject.transform.position = targetPosition;
            playerAnim.gameObject.transform.rotation = targetRotation;
            StartTeleportSequence();
        }
    }

    private void StartTeleportSequence()
    {
        playerAnim.gameObject.GetComponent<PlayerFighter>().InAction = true;
        UIStateManager.SetUIActive(true);
        if (playerAnim != null)
            playerAnim.CrossFade("TeleportEnter", 0.1f);

        StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        yield return new WaitForSeconds(2f);
        teleportLight.SetTrigger("StartTeleport");
    //    if (isTeleporting) yield break;
    //    isTeleporting = true;

    //    // �ȴ��������ţ�����еĻ���
    //    if (playerAnim != null)
    //        yield return new WaitForSeconds(5.0f); // ����ʱ��Ϊ��������

    //    // ������Ϸ����
    //    if (SaveManager.Instance != null)
    //    {
    //        if (hasTriggered) yield break;
    //        hasTriggered = true;

    //        SaveManager.Instance.SaveGame();
    //        SaveManager.Instance.currentSaveData.currentScene = gameSceneName;
    //        SaveManager.isNewGame = false;
    //    }

    //    // ����Ŀ�곡��������
    //    PlayerPrefs.SetString("TargetScene", gameSceneName);
    //    SaveManager.shouldLoadFromSave = true;
    //    SaveManager.shouldLoadPosition = false;

    //    // ���س���
    //    SceneManager.LoadScene(loadingSceneName);
    }
}
