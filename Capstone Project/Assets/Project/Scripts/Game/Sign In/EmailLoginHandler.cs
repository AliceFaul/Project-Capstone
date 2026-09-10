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
                await StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>().EmailLogin(email, password).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    { 
                        Debug.LogError($"[EmailLoginHandler] Login failed: {task.Exception}]");
                    }
                    else 
                    { 
                        Debug.Log($"[EmailLoginHandler] Login succeeded: {task.Result}"); 
                        PlayerPrefs.SetString("SAVED_EMAIL", email); 
                        PlayerPrefs.SetString("SAVED_PASSWORD", password); 
                        PlayerPrefs.Save();
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[EmailLoginHandler] Login exception occured: {e.Message}");
            }
            
            AuthUIHandler.Instance.SetLoadingState(false);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}