using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Luồng giao diện rương Lobby (bản gọn, chưa có logic Gem / vũ khí):
// click NPC -> mở giao diện -> rương chạy Intro rồi về Idle -> bấm Mở x1 / x10 -> rương chạy Opening
//giữ vài giây -> về Idle.
public class GachaPanelController : MonoBehaviour
{
    public static GachaPanelController Instance { get; private set; }

    public event Action<int> OnOpenRequested;

    [Header("Tham chiếu")]
    [Tooltip("Object sẽ được bật/tắt khi mở giao diện. Phải là object cha NGOÀI CÙNG đang bị tắt (chữ xám) trong Hierarchy, VD: Panelchest.")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GachaChestVisual chestVisual;
    [SerializeField] private Button btnOpenX1;
    [SerializeField] private Button btnOpenX10;
    [SerializeField] private Button btnClose;

    [Header("Test")]
    [SerializeField] private Color testBurstColor = new Color(1f, 0.76f, 0.03f);
    [Tooltip("Sau khi rương mở xong, giữ tư thế mở bấy nhiêu giây rồi mới về Idle.")]
    [SerializeField] private float holdOpenTime = 1.5f;

    private bool busy;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (btnOpenX1 != null) btnOpenX1.onClick.AddListener(() => OnClickOpen(1));
        if (btnOpenX10 != null) btnOpenX10.onClick.AddListener(() => OnClickOpen(10));
        if (btnClose != null) btnClose.onClick.AddListener(ClosePanel);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OpenPanel()
    {
        if (panelRoot == null || IsOpen)
            return;

        panelRoot.SetActive(true);

        if (!panelRoot.activeInHierarchy)
            WarnInactiveParent();

        busy = false;
        SetButtonsInteractable(true);

        if (chestVisual != null)
            chestVisual.PlayIntro();   // Intro -> tự chuyển sang Idle, chờ người chơi chọn
    }

    public void ClosePanel()
    {
        if (busy || panelRoot == null)
            return;

        if (chestVisual != null)
            chestVisual.ResetToClosed();

        panelRoot.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        Debug.Log($"[GachaPanel] {message}");
    }

    private void OnClickOpen(int count)
    {
        if (busy)
            return;

        StartCoroutine(OpenRoutine(count));
    }

    private IEnumerator OpenRoutine(int count)
    {
        busy = true;
        SetButtonsInteractable(false);

        OnOpenRequested?.Invoke(count);
        Debug.Log($"[GachaPanel] Mở x{count}: chạy animation Opening.");

        if (chestVisual != null)
            yield return chestVisual.PlayOpen(testBurstColor);

        yield return new WaitForSeconds(holdOpenTime);

        if (chestVisual != null)
            chestVisual.ResetToClosed();

        busy = false;
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (btnOpenX1 != null) btnOpenX1.interactable = value;
        if (btnOpenX10 != null) btnOpenX10.interactable = value;
        if (btnClose != null) btnClose.interactable = value;
    }

    private void WarnInactiveParent()
    {
        Transform t = panelRoot.transform.parent;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
            {
                Debug.LogWarning($"[GachaPanel] '{t.name}' (cha của Panel Root) đang bị TẮT nên giao diện không hiện. " +
                                 $"Kéo '{t.name}' vào ô Panel Root thay cho '{panelRoot.name}'.");
                return;
            }

            t = t.parent;
        }
    }
}