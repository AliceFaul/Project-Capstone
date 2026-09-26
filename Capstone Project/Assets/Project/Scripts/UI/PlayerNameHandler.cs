using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

public class PlayerNameHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private LocalizationText errorMessage;

    private readonly LocalizedString _nameNullOrWhiteSpace = new LocalizedString("UI", "NAME_NULL_OR_WHITESPACE");
    private readonly LocalizedString _nameTooLong = new LocalizedString("UI", "NAME_TOO_LONG");
    private readonly LocalizedString _defaultError = new LocalizedString("UI", "NAME_INVALID");

    private PlayerDataConfig _config;

    public void Initialize(PlayerDataConfig config)
    {
        _config = config;
        errorMessage?.InitText();
        if(_config != null && nameInput != null) nameInput.text = (_config.DisplayName == "Unknown") ? string.Empty : _config.DisplayName;
    }

    public bool OnConfirm()
    {
        if(_config == null) return false;
        
        string newName = nameInput != null ?  nameInput.text : string.Empty;
        bool success = _config.SetDisplayName(newName, out string errorId);

        if (success) return true;
        
        ShowError(errorId);
        return false;
    }

    private void ShowError(string errorId)
    {
        if(errorMessage == null) return;

        LocalizedString message = errorId switch
        {
            "NAME_NULL_OR_WHITESPACE" => _nameNullOrWhiteSpace,
            "NAME_TOO_LONG" => _nameTooLong,
            _ => _defaultError
        };
        
        errorMessage.ChangeText(message);
    }
}