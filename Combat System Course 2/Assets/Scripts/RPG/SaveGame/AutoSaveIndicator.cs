using TMPro;
using UnityEngine;

public class AutoSaveIndicator : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject indicatorRoot;
    [SerializeField] private RectTransform iconTransform;
    [SerializeField] private TMP_Text statusText;

    [Header("Settings")]
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float spinDuration = 1.5f;
    [SerializeField] private float savedShowDuration = 1.5f;
    [SerializeField] private string savingText = "";
    [SerializeField] private string savedText = "";

    private enum State { Hidden, Spinning, ShowingSaved }
    private State state;
    private float timer;
    private bool saveCompleted;

    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnAutoSaveStart += OnSaveStart;
            SaveManager.Instance.OnAutoSaveComplete += OnSaveComplete;
        }

        if (indicatorRoot != null)
            indicatorRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnAutoSaveStart -= OnSaveStart;
            SaveManager.Instance.OnAutoSaveComplete -= OnSaveComplete;
        }
    }

    private void OnSaveStart()
    {
        if (indicatorRoot != null)
            indicatorRoot.SetActive(true);

        if (statusText != null)
            statusText.text = savingText;

        state = State.Spinning;
        timer = 0f;
        saveCompleted = false;
    }

    private void OnSaveComplete()
    {
        saveCompleted = true;
    }

    private void Update()
    {
        if (indicatorRoot == null || !indicatorRoot.activeSelf) return;

        timer += Time.deltaTime;

        switch (state)
        {
            case State.Spinning:
                if (iconTransform != null)
                    iconTransform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

                if (timer >= spinDuration && saveCompleted)
                {
                    if (statusText != null)
                        statusText.text = savedText;

                    state = State.ShowingSaved;
                    timer = 0f;
                }
                break;

            case State.ShowingSaved:
                if (timer >= savedShowDuration)
                {
                    indicatorRoot.SetActive(false);
                    state = State.Hidden;
                }
                break;
        }
    }
}
