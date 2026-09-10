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
        if(loginButton == null)
            loginButton = GetComponent<Button>();
        
        loginButton?.onClick.AddListener(OnLoginClicked);
    }

    private void OnLoginClicked()
    {
        if (emailInputField == null || passwordInputField == null)
        { 
            Debug.LogError($"[EmailLoginHandler] Email field or Password field is empty");
            return;
        }

        string email = emailInputField.text;
        string password = passwordInputField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError($"[EmailLoginHandler] Email field or password field is empty]");
            return;
        }
        
        StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>().EmailLogin(email, password).ContinueWith(task =>
        {
            if(task.IsFaulted)
                Debug.LogError($"[EmailLoginHandler] Login failed: {task.Exception}]");
            else
                Debug.Log($"[EmailLoginHandler] Login succeeded: {task.Result}");
        });
    }
}