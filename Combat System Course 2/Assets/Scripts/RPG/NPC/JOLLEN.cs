using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JOLLEN : MonoBehaviour
{
    public ItemSO witchKiller;
    public Quest caveQuestToComplete;

    public void CompleteCaveMission()
    {
        InventoryManager.Instance.AddItem(witchKiller);
        GameManager.Instance.SetQuestState(caveQuestToComplete, QuestState.Completed);
    }
}
