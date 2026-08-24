using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Blox Fruits Buttons")]
    [SerializeField] private GameObject nextButton;    // Kéo NextButton vào 
    [SerializeField] private GameObject acceptButton;  // Kéo AcceptButton vào 
    [SerializeField] private GameObject declineButton; // Kéo DeclineButton vào 

    [Header("Cấu hình Chạy chữ")]
    [SerializeField] private float typingSpeed = 0.03f;

    private Queue<string> sentences = new Queue<string>();
    private bool isTyping = false;
    private string currentSentence;
    private QuestData currentQuest; // Lưu QuestData hiện tại từ NPC
    private NPC currentNPC;         // Lưu NPC hiện tại đang tương tác
    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        dialoguePanel.SetActive(false);
        HideAllButtons();
    }

    private void Update()
    {
        if (IsDialogueActive && Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = currentSentence;
                isTyping = false;
                ShowCorrectButtons();
            }
        }
    }

    // Đã thêm tham số NPC npc = null để lưu lại tham chiếu NPC đang tương tác
    public void StartDialogue(string npcName, string[] dialogueSentences, QuestData questData = null, NPC npc = null)
    {
        IsDialogueActive = true;
        dialoguePanel.SetActive(true);
        nameText.text = npcName;
        currentQuest = questData; // Nhận QuestData truyền vào
        currentNPC = npc;         // Nhận NPC truyền vào

        sentences.Clear();
        foreach (string sentence in dialogueSentences)
        {
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        HideAllButtons();
        currentSentence = sentences.Dequeue();

        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        ShowCorrectButtons();
    }

    private void ShowCorrectButtons()
    {
        if (sentences.Count > 0)
        {
            if (nextButton != null) nextButton.SetActive(true);
            SetChoiceButtonsActive(false);
        }
        else
        {
            if (nextButton != null) nextButton.SetActive(false);
            SetChoiceButtonsActive(true);
        }
    }

    private void SetChoiceButtonsActive(bool isActive)
    {
        if (acceptButton != null) acceptButton.SetActive(isActive);
        if (declineButton != null) declineButton.SetActive(isActive);
    }

    private void HideAllButtons()
    {
        if (nextButton != null) nextButton.SetActive(false);
        SetChoiceButtonsActive(false);
    }

    public void EndDialogue()
    {
        IsDialogueActive = false;
        dialoguePanel.SetActive(false);
        HideAllButtons();
    }

    public void OnAcceptClicked()
    {
        Debug.Log("Đã CHẤP NHẬN nhiệm vụ!");

        // Truyền quest sang PlayerQuestManager (nếu có)
        if (currentQuest != null && PlayerQuestManager.Instance != null)
        {
            PlayerQuestManager.Instance.AcceptQuest(currentQuest);
        }
       
        // Gọi NPC đổi trạng thái sang đứng yên
        if (currentNPC != null)
        {
            currentNPC.SetIdleState();
        }

        EndDialogue();
    }

    public void OnDeclineClicked()
    {
        Debug.Log("Đã TỪ CHỐI nhiệm vụ!");
        EndDialogue();
    }
}