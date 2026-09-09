using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button itemButton;

    // Sửa tham số QuestData thành ActiveQuest
    public void Setup(ActiveQuest activeQuest, Action<ActiveQuest> onClicked)
    {
        if (activeQuest == null || activeQuest.data == null) return;

        if (titleText != null)
        {
            titleText.text = activeQuest.data.questTitle;
        }

        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() => onClicked?.Invoke(activeQuest));
        }
    }
}