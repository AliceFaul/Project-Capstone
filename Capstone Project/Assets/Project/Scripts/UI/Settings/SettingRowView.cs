using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SettingRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler
{
    private static readonly Color FrameOff = new Color(1f, 1f, 1f, 0f);
    private static readonly Color FrameOn = Color.white;
    private static readonly Color FrameListening = new Color(0.94f, 0.50f, 0.16f);
    private static readonly Color PillOff = new Color(0.145f, 0.275f, 0.353f);
    private static readonly Color PillOn = new Color(0.94f, 0.50f, 0.16f);

    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text label;

    [Header("Selector")]
    [SerializeField] private GameObject selectorRoot;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text selectorValue;

    [Header("Toggle")]
    [SerializeField] private GameObject toggleRoot;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleState;
    [SerializeField] private Image pill;
    [SerializeField] private RectTransform knob;

    [Header("Slider")]
    [SerializeField] private GameObject sliderRoot;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text sliderValue;

    [Header("Key")]
    [SerializeField] private GameObject keyRoot;
    [SerializeField] private Button keyButton;
    [SerializeField] private TMP_Text keyText;

    [Header("Button")]
    [SerializeField] private GameObject buttonRoot;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionLabel;

    private enum Kind { None, Selector, Toggle, Slider, Key, Button }

    public string Title { get; private set; }
    public string Description { get; private set; }
    
    // Toggle / key / button rows react to Enter; selector and slider rows do not (Enter is "Apply" on the Video tab).
    public bool HandlesSubmit => _kind == Kind.Toggle || _kind == Kind.Key || _kind == Kind.Button;

    public event Action<SettingRowView> Focused;

    private Kind _kind;
    private IList<string> _options;
    private int _index;
    private Action<int> _onSelector;
    private bool _toggleValue;
    private Action<bool> _onToggle;
    private Func<float, string> _format;
    private Action<float> _onSlider;
    private bool _suppressSlider;
    private Action _onClick;
    private bool _hover;
    private bool _focus;
    private bool _listening;

    private void Awake()
    {
        var root = GetComponent<Button>();
        root.transition = Selectable.Transition.None;
        root.navigation = new Navigation { mode = Navigation.Mode.Vertical };
        root.onClick.AddListener(Activate);

        // the controls inside are mouse targets only: keyboard focus stays on the row
        foreach (var inner in GetComponentsInChildren<Selectable>(true))
        {
            if (inner != root) inner.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        prevButton.onClick.AddListener(() => Step(-1));
        nextButton.onClick.AddListener(() => Step(1));
        toggleButton.onClick.AddListener(FlipToggle);
        slider.onValueChanged.AddListener(OnSliderChanged);
        keyButton.onClick.AddListener(() => _onClick?.Invoke());
        actionButton.onClick.AddListener(() => _onClick?.Invoke());
    }

    private void OnDisable()
    {
        _hover = false;
        _focus = false;
        RefreshFrame();
    }

    // ---------------------------------------------------------------- setup

    private void Begin(Kind kind, string title, string description)
    {
        _kind = kind;
        Title = title;
        Description = description;
        label.text = title;

        selectorRoot.SetActive(kind == Kind.Selector);
        toggleRoot.SetActive(kind == Kind.Toggle);
        sliderRoot.SetActive(kind == Kind.Slider);
        keyRoot.SetActive(kind == Kind.Key);
        buttonRoot.SetActive(kind == Kind.Button);
    }

    public void SetupSelector(string title, string description, IList<string> options, int index, Action<int> onChanged)
    {
        Begin(Kind.Selector, title, description);
        _options = options;
        _onSelector = onChanged;
        _index = options.Count > 0 ? Mathf.Clamp(index, 0, options.Count - 1) : 0;
        selectorValue.text = options.Count > 0 ? options[_index] : "-";
    }

    public void SetupToggle(string title, string description, bool value, Action<bool> onChanged)
    {
        Begin(Kind.Toggle, title, description);
        _onToggle = onChanged;
        SetToggleVisual(value);
    }

    public void SetupSlider(string title, string description, float min, float max, float value, bool whole,
        Func<float, string> format, Action<float> onChanged)
    {
        Begin(Kind.Slider, title, description);
        _format = format;
        _onSlider = onChanged;

        _suppressSlider = true;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = whole;
        slider.value = Mathf.Clamp(value, min, max);
        _suppressSlider = false;
        sliderValue.text = format(slider.value);
    }

    public void SetupKey(string title, string description, string keyName, Action onClick)
    {
        Begin(Kind.Key, title, description);
        _onClick = onClick;
        SetKeyText(keyName, false);
    }

    public void SetupButton(string title, string description, string buttonText, Action onClick)
    {
        Begin(Kind.Button, title, description);
        _onClick = onClick;
        actionLabel.text = buttonText;
    }

    public void SetKeyText(string text, bool listening)
    {
        _listening = listening;
        keyText.text = text;
        keyText.color = listening ? new Color(1f, 0.82f, 0.48f) : Color.white;
        RefreshFrame();
    }

    private void Activate()
    {
        switch (_kind)
        {
            case Kind.Toggle: FlipToggle(); break;
            case Kind.Key:
            case Kind.Button: _onClick?.Invoke(); break;
        }
    }

    private void Step(int direction)
    {
        if (_options == null || _options.Count == 0) return;

        _index = (_index + direction + _options.Count) % _options.Count;
        selectorValue.text = _options[_index];
        _onSelector?.Invoke(_index);
    }

    private void FlipToggle()
    {
        SetToggleVisual(!_toggleValue);
        _onToggle?.Invoke(_toggleValue);
    }

    private void SetToggleVisual(bool value)
    {
        _toggleValue = value;
        toggleState.text = InventoryText.Get(value ? "SET_ON" : "SET_OFF");
        pill.color = value ? PillOn : PillOff;
        knob.anchoredPosition = new Vector2(value ? 78f : 18f, 0f);
    }

    private void OnSliderChanged(float value)
    {
        if (_suppressSlider) return;
        sliderValue.text = _format(value);
        _onSlider?.Invoke(value);
    }

    private void RefreshFrame() => frame.color = _listening ? FrameListening : (_hover || _focus ? FrameOn : FrameOff);

    public void OnPointerEnter(PointerEventData eventData) { _hover = true; RefreshFrame(); Focused?.Invoke(this); }
    public void OnPointerExit(PointerEventData eventData) { _hover = false; RefreshFrame(); }
    public void OnSelect(BaseEventData eventData) { _focus = true; RefreshFrame(); Focused?.Invoke(this); }
    public void OnDeselect(BaseEventData eventData) { _focus = false; RefreshFrame(); }

    public void OnMove(AxisEventData eventData)
    {
        int direction = eventData.moveDir == MoveDirection.Left ? -1 : eventData.moveDir == MoveDirection.Right ? 1 : 0;
        if (direction == 0) return;

        if (_kind == Kind.Selector)
        {
            Step(direction);
            eventData.Use();
        }
        else if (_kind == Kind.Slider)
        {
            slider.value = Mathf.Clamp(slider.value + direction * (slider.maxValue - slider.minValue) / 20f, slider.minValue, slider.maxValue);
            eventData.Use();
        }
    }

    public void OnSubmit(BaseEventData eventData) => Activate();
}