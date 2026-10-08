using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class StartupErrorController
{
    private const string TableName = "UI";
    private const string FallbackError = "UI_GENERIC_ERROR";

    public void ThrowError(string errorId)
    {
        var initOp = LocalizationSettings.InitializationOperation;
        if (!initOp.IsDone)
        {
            initOp.Completed += _ => ShowError(errorId);
        }
        else
        {
            ShowError(errorId);
        }
    }
    
    private void ShowError(string errorId)
    {
        string dynamicKey = $"UI_{errorId}";
        
        var table = LocalizationSettings.StringDatabase.GetTable(TableName);
        LocalizedString localized;

        if (table == null)
        {
            Debug.LogWarning($"[Localization] StringTable {TableName} is null. Fallback {FallbackError}");
            localized = new LocalizedString(TableName, FallbackError);
        }
        else
        {
            var entry = table.GetEntry(dynamicKey);
            if (entry == null)
            {
                Debug.LogWarning($"[Localization] Entry {dynamicKey} is null. Fallback {FallbackError}");
                localized = new LocalizedString(TableName, FallbackError);
            }
            else
            {
                localized = new LocalizedString(TableName, dynamicKey);
            }
        }

        UIManager.Instance?.GetPopupService().Create("ErrorPopup", errorId, localized);
    }
}