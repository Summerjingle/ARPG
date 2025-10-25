using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CaveToMountain : MonoBehaviour
{
    private bool isPlayerInTrigger=false;
    public AudioClip doorOpenSound;
    public GameObject openDoorTipText;
    public string gameSceneName = "02Scene_Mountain";
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
            AudioSource.PlayClipAtPoint(doorOpenSound, transform.position);
            // 保存游戏状态
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                SaveManager.isNewGame = false;
            }

            // 设置目标场景并加载
            PlayerPrefs.SetString("TargetScene", gameSceneName);
            SaveManager.shouldLoadFromSave = true;
            SceneManager.LoadScene(loadingSceneName);
        }
    }
}
