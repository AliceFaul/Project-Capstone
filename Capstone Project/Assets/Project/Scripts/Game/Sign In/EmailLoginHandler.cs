using System;
using TMPro;
using UnityEngine;
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
                Debug.LogError($"[EmailLoginHandler] Email field or Password field is empty");
                return;
            }

            string email = emailInputField.text.Trim();
            string password = passwordInputField.text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Debug.LogError($"[EmailLoginHandler] Email field or password field is empty]");
                return;
            }
            
            AuthUIHandler.Instance.SetLoadingState(true);

            try
            {
                var authService = StartupProcessor.Instance.GetService<PlayFabServiceManager>()
                    .GetService<PlayFabAuthentication>();
                bool success = await authService.EmailLogin(email, password);
                if (success)
                {
                    PlayerPrefs.SetString("SAVED_EMAIL", email);
                    PlayerPrefs.SetString("SAVED_PASSWORD", password);
                    PlayerPrefs.Save();
                    AuthUIHandler.Instance.OnAuthSuccess();
                }
                else
                {
                    Debug.LogError($"[EmailLoginHandler] Failed to login {email} {password}.");
                    AuthUIHandler.Instance.SetLoadingState(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EmailLoginHandler] Login exception occured: {e.Message}");
                AuthUIHandler.Instance.SetLoadingState(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}