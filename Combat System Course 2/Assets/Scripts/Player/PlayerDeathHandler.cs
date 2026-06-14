using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private GameObject youDiedPanel;
    [SerializeField] private float deathDelay = 2f;
    [SerializeField] private string loadingSceneName = "LoadingScene";

    private HealthSystem healthSystem;

    private void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null)
            healthSystem.OnDeath += HandleDeath;

        if (youDiedPanel != null)
            youDiedPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnDeath -= HandleDeath;
    }

    private void HandleDeath(HealthSystem hs)
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        InputManager.Instance?.SwitchToEmpty();

        if (youDiedPanel != null)
            youDiedPanel.SetActive(true);

        yield return new WaitForSeconds(deathDelay);

        if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData != null && !SaveManager.isNewGame)
        {
            bool hasCheckpoint = !string.IsNullOrEmpty(SaveManager.Instance.currentSaveData.currentScene);
            SaveManager.currentSaveId = SaveManager.Instance.currentSaveData.saveId;
            SaveManager.Instance.currentSaveData.currentScene = SceneManager.GetActiveScene().name;
            SaveManager.shouldLoadPosition = hasCheckpoint;
            SaveManager.shouldLoadFromSave = true;
        }

        if (youDiedPanel != null)
            youDiedPanel.SetActive(false);

        SceneManager.LoadScene(loadingSceneName);
    }
}
