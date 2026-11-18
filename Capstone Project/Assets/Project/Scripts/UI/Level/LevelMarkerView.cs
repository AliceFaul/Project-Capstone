using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A level on the map of the level select screen.
[RequireComponent(typeof(Button))]
public class LevelMarkerView : MonoBehaviour
{
    private static readonly Color Selected = new Color(0.94f, 0.50f, 0.16f);
    private static readonly Color Open = new Color(0.62f, 0.71f, 0.75f);
    private static readonly Color Closed = new Color(0.30f, 0.40f, 0.45f);

    [SerializeField] private Image ring;
    [SerializeField] private GameObject bracket;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text lockText;
    [SerializeField] private CanvasGroup group;

    public LevelData Level { get; private set; }
    public event Action<LevelMarkerView> Clicked;

    private void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));

    public void Bind(LevelData level, bool unlocked, bool selected)
    {
        Level = level;
        label.text = level.GetName();
        lockText.gameObject.SetActive(!unlocked);
        lockText.text = InventoryText.Get("LEVEL_LOCKED");
        bracket.SetActive(selected);
        group.alpha = unlocked ? 1f : 0.65f;
        ring.color = selected ? Selected : (unlocked ? Open : Closed);
    }
}