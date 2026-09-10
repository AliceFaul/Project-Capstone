using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailRegisterHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_InputField confirmPasswordInputField;
    [SerializeField] private Button registerButton;

    private void Awake()
    {
        if(registerButton == null) registerButton = GetComponent<Button>();
        registerButton?.onClick.AddListener(OnRegisterClicked);
    }

    private async void OnRegisterClicked()
    {
        try
        {
            if (usernameInputField == null || emailInputField == null || 
                passwordInputField == null || confirmPasswordInputField == null)
            {
                Debug.LogError($"[EmailRegisterHandler] Input fields is missing! Please implement to continue.");
                return;
            }
        
            string  username = usernameInputField.text.Trim();
            string email = emailInputField.text.Trim();
            string password = passwordInputField.text.Trim();
            string confirmPassword = confirmPasswordInputField.text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                Debug.LogError($"[EmailRegisterHandler] All fields are required! Please fill in all fields.");
                return;
            }

            if (password != confirmPassword)
            {
                Debug.LogError($"[EmailRegisterHandler] Passwords do not match! Please check again.");
                return;
            }

            AuthUIHandler.Instance.SetLoadingState(true);
            
            try
            {
                await StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>().EmailRegister(email, password, username).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"[EmailRegisterHandler] An error occured while registering user {username}! Error: " + task.Exception?.GetBaseException().Message);
                    }
                    else
                    {
                        Debug.Log($"[EmailRegisterHandler] Registered user {username} successfully!");
                        PlayerPrefs.SetString("SAVED_USERNAME", username);
                        PlayerPrefs.SetString("SAVED_EMAIL", email);
                        PlayerPrefs.SetString("SAVED_PASSWORD", password);
                        PlayerPrefs.Save();
                    }
                });
                
                AuthUIHandler.Instance.SetLoadingState(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EmailRegisterHandler] Registering user {username} failed! Error: {e.Message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}