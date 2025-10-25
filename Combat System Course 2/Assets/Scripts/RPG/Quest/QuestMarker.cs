using UnityEngine;

public class QuestMarker : MonoBehaviour
{
    [Header("Quest Reference")]
    public Quest quest;

    [Header("Marker Models")]
    public GameObject exclamationMark; // £¡
    public GameObject starMark;        // ¡ï

    [Header("Settings")]
    public bool updateInRealTime = true;
    public float updateInterval = 0.3f;

    private float updateTimer;

    private void Start()
    {
        HideAllMarkers();
        UpdateVisibility();
    }

    private void Update()
    {
        if (updateInRealTime)
        {
            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0)
            {
                UpdateVisibility();
                updateTimer = updateInterval;
            }
        }
    }

    public void UpdateVisibility()
    {
        if (GameManager.Instance == null || quest == null)
        {
            HideAllMarkers();
            return;
        }

        QuestState state = GameManager.Instance.GetQuestState(quest);

        switch (state)
        {
            case QuestState.NotAccepted:
                ShowExclamation();
                break;
            case QuestState.InProgress:
                HideAllMarkers();
                break;
            case QuestState.CanComplete:
                ShowStar();
                break;
            case QuestState.Completed:
                HideAllMarkers();
                break;
        }
    }

    private void ShowExclamation()
    {
        SetMarkers(true, false);
    }

    private void ShowStar()
    {
        SetMarkers(false, true);
    }

    private void HideAllMarkers()
    {
        SetMarkers(false, false);
    }

    private void SetMarkers(bool showExclamation, bool showStar)
    {
        if (exclamationMark != null)
            exclamationMark.SetActive(showExclamation);
        if (starMark != null)
            starMark.SetActive(showStar);
    }

    public void Refresh()
    {
        UpdateVisibility();
    }
}