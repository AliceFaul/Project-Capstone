using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;

public class LocalizationText : MonoBehaviour, ITextProvider
{
    [SerializeField] private LocalizedString _localizedString;

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

        _localizedString.StringChanged += (localizedString) =>
        {
            _text.text = localizedString;
        };
    }

    public void SetArguments(List<string> value)
    {
        _localizedString.Arguments = value.ToArray();
    }

    public void ChangeText(LocalizedString text)
    {
        if(_localizedString != null)
            _localizedString.StringChanged -= UpdateText;

        _localizedString = text;

        if(_localizedString != null)
            _localizedString.StringChanged += UpdateText;

        _localizedString?.RefreshString();
    }

    private void UpdateText(string value)
    {
        _text.text = value;
    }
}