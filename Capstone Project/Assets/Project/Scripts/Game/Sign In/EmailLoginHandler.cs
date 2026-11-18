using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EmailLoginHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Button loginButton;

    private void Awake()
    {
        if(loginButton == null) loginButton = GetComponent<Button>();
        loginButton?.onClick.AddListener(OnLoginClicked);
    }

    private async void OnLoginClicked()
    {
        try
        {
            if (emailInputField == null || passwordInputField == null)
            { 
                Debug.LogError($"[EmailLoginHandler] UI References missing.");
                return;
            }

            string email = emailInputField.text.Trim();
            string password = passwordInputField.text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return;
            }
            
            AuthUIHandler.Instance.SetLoadingState(true);

            var authService = StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>();
            bool success = await authService.EmailLogin(email, password);
            
            if (success)
            {
                CryptoUtils.SaveCredentials(email, password);
                AuthUIHandler.Instance.OnAuthSuccess();
            }
            else
            {
                Debug.LogError($"[EmailLoginHandler] Failed to login {email}.");
                EventManager.Instance?.Trigger("ON_AUTH_FAILED"); 
                AuthUIHandler.Instance.SetLoadingState(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[EmailLoginHandler] Login critical exception: {e.Message}");
            AuthUIHandler.Instance.SetLoadingState(false);
        }
    }
}