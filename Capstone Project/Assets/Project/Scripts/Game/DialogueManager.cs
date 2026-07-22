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
    [SerializeField] private GameObject continuePrompt; // Icon ClickPrompt 

    [Header("Cấu hình Chạy chữ")]
    [SerializeField] private float typingSpeed = 0.03f; // Tốc độ hiện từng chữ

    private Queue<string> sentences = new Queue<string>();
    private bool isTyping = false;
    private string currentSentence;
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
        if (continuePrompt != null) continuePrompt.SetActive(false);
    }

    private void Update()
    {
        // Bấm chuột trái khi đang thoại
        if (IsDialogueActive && Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // Nếu chữ đang chạy mà click => Hiện luôn toàn bộ câu thoại
                StopAllCoroutines();
                dialogueText.text = currentSentence;
                isTyping = false;
                if (continuePrompt != null) continuePrompt.SetActive(true);
            }
            else
            {
                // Nếu chữ hiện xong mà click => Sang câu tiếp theo
                DisplayNextSentence();
            }
        }
    }

    public void StartDialogue(string npcName, string[] dialogueSentences)
    {
        IsDialogueActive = true;
        dialoguePanel.SetActive(true);
        nameText.text = npcName;

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

        currentSentence = sentences.Dequeue();
        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        if (continuePrompt != null) continuePrompt.SetActive(false);

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }

    public void EndDialogue()
    {
        IsDialogueActive = false;
        dialoguePanel.SetActive(false);
    }

    // xử lý khi bấm nút Chấp nhận
    public void OnAcceptClicked()
    {
        Debug.Log("Người chơi đã CHẤP NHẬN!");
        EndDialogue(); // Đóng hộp thoại
    }

    // xử lý khi bấm nút Từ chối
    public void OnDeclineClicked()
    {
        Debug.Log("Người chơi đã TỪ CHỐI!");
        EndDialogue(); // Đóng hộp thoại
    }
}