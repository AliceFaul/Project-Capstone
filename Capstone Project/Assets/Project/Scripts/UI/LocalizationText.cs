using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;

public class LocalizationText : MonoBehaviour, ITextProvider
{
    [SerializeField] private LocalizedString localizedString;

    [SerializeField] private TMP_Text text;
    
    private void Awake()
    {
        if(text == null) text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if(localizedString != null) localizedString.StringChanged += UpdateText;
    }

    private void OnDisable()
    {
        if(localizedString != null) localizedString.StringChanged -= UpdateText;
    }

    public void InitText()
    {
        if (text == null)
        {
            Debug.LogError($"[LocalizationText] {gameObject.name} has no TMP_Text component!");
            return;
        }

        localizedString.StringChanged += (value) =>
        {
            text.text = value;
        };
    }

    public void SetArguments(List<string> value)
    {
        localizedString.Arguments = value.ToArray();
    }

    public void ChangeText(LocalizedString valueText)
    {
        if(localizedString != null) localizedString.StringChanged -= UpdateText;
        localizedString = valueText;
        if(localizedString != null) localizedString.StringChanged += UpdateText;
        localizedString?.RefreshString();
    }

    private void UpdateText(string value)
    {
        if (!this || !gameObject) return;
        
        if(text != null) text.text = value;
        else Debug.LogError($"[LocalizationText] {gameObject.name} has no TMP_Text!");
    }
}