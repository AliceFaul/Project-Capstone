using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;

public class LocalizationText : MonoBehaviour, ITextProvider
{
    [SerializeField] private LocalizedString localizedString;

    private TMP_Text _text;
    
    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    public void InitText()
    {
        if (_text == null)
        {
            Debug.LogError($"[LocalizationText] {gameObject.name} has no TMP_Text component!");
            return;
        }

        localizedString.StringChanged += (value) =>
        {
            _text.text = value;
        };
    }

    public void SetArguments(List<string> value)
    {
        localizedString.Arguments = value.ToArray();
    }

    public void ChangeText(LocalizedString text)
    {
        if(localizedString != null)
            localizedString.StringChanged -= UpdateText;

        localizedString = text;

        if(localizedString != null)
            localizedString.StringChanged += UpdateText;

        localizedString?.RefreshString();
    }

    private void UpdateText(string value)
    {
        _text.text = value;
    }
}