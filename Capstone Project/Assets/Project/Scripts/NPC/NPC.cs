using UnityEngine;

/// Interactable NPC: the player clicks it (PlayerController walks over, then calls OnInteract).
public class NPC : Interactable
{
    [Header("NPC Info")]
    [SerializeField] private string npcName = "Blacksmith";
    [SerializeField] private QuestData questData;
    [SerializeField] private bool hasGivenQuest = false;

    [Header("Dialogue")]
    [TextArea(3, 5)]
    [SerializeField] private string[] sentences;

    public string NpcName { get => npcName; set => npcName = value; }
    public string[] Sentences  { get => sentences; set => sentences = value; }

    protected override void OnInteract(PlayerController player)
    {
        if(DialogueManager.Instance == null) return;
        if(DialogueManager.Instance.IsDialogueActive) return;
        
        if(!hasGivenQuest) return;
        DialogueManager.Instance.StartDialogue(npcName, sentences, questData, this);
    }
}