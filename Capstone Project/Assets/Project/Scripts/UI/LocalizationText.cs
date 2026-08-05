using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;

public class LocalizationText : MonoBehaviour, ITextProvider
{
    [SerializeField] private LocalizedString _localizedString;

    private void Awake()
    {
        InitText();
    }

    public void InitText()
    {
        TMP_Text text = GetComponent<TMP_Text>();

        if (text == null)
        {
            Debug.LogError($"[LocalizationText] {gameObject.name} has no TMP_Text component!");
            return;
        }

        _localizedString.StringChanged += (localizedString) =>
        {
            text.text = localizedString;
        };
    }

    public void SetArguments(List<string> value)
    {
        _localizedString.Arguments = value.ToArray();
    }

    public void ChangeText(LocalizedString text)
    {
        _localizedString = text;
        InitText();
    }
}