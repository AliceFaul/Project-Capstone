using UnityEngine;
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

    private IPlayerIdentity _identity;

    public void Initialize(IPlayerIdentity identity)
    {
        _identity = identity;
        errorMessage?.InitText();
        if(_identity != null && nameInput != null) nameInput.text = (_identity.DisplayName == "Unknown") ? string.Empty : _identity.DisplayName;
    }

    public bool OnConfirm()
    {
        if(_identity == null) return false;
        
        string newName = nameInput != null ?  nameInput.text : string.Empty;
        bool success = _identity.SetDisplayName(newName, out string errorId);

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