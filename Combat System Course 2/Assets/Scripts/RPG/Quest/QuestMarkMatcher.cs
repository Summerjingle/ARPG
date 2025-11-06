using UnityEngine;

public class QuestMarkMatcher : MonoBehaviour
{
    public Quest relatedQuest;
    public GameObject minimapMarker;

    private void Update()
    {
        if (minimapMarker != null && relatedQuest != null)
        {
            
            bool shouldNotShow = QuestManager.Instance.IsQuestAccepted(relatedQuest);
            minimapMarker.SetActive(!shouldNotShow);
        }
    }
}