using System.Collections.Generic;
using UnityEngine.Localization;

public interface ITextProvider
{
    void InitText();
    void SetArguments(List<string> value);
    void ChangeText(LocalizedString valueText);
}