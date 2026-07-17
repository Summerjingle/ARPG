using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CaveToMountain : MonoBehaviour
{
    private bool isPlayerInTrigger=false;
    public AudioClip doorOpenSound;
    public GameObject openDoorTipText;
    public string gameSceneName = "02Scene_HighPlace";
    public string loadingSceneName = "LoadingScene";
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            openDoorTipText.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            openDoorTipText.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            AudioManager.Instance.PlaySFX(doorOpenSound, transform.position);
            // ������Ϸ״̬
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                SaveManager.Instance.currentSaveData.currentScene = gameSceneName;
                SaveManager.isNewGame = false;
            }

            // ����Ŀ�곡��������
            PlayerPrefs.SetString("TargetScene", gameSceneName);
            SaveManager.shouldLoadFromSave = true;
            SaveManager.shouldLoadPosition = false;
            SceneManager.LoadScene(loadingSceneName);
        }
    }
}
