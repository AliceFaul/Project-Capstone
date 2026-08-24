using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button button;

    private QuestData questData;
    private QuestLogUI questLogUI;

    public void Setup(QuestData quest, QuestLogUI logUI)
    {
        questData = quest;
        questLogUI = logUI;
        titleText.text = quest.questTitle;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => questLogUI.ShowQuestDetails(questData));
    }
}