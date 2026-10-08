using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMenuNavigate : MonoBehaviour
{
    [SerializeField] private GameObject inventoryScreen;
    [SerializeField] private Button inventoryButton;
    
    private InputHandler _inputHandler;
    private GameObject _currentScreen;

    private void Awake()
    {
        _inputHandler = GetComponent<InputHandler>();
    }

    private void Start()
    {
        inventoryButton?.onClick.AddListener(() => Toggle(inventoryScreen));
    }

    private void OnEnable()
    {
        if (_inputHandler == null)
        {
            Debug.LogError($"[PlayerMenuNavigate] Not found InputHandler in {gameObject.name}]");
            return;
        }

        _inputHandler.OnToggleInventory += OnToggleInventory;
    }

    private void OnDisable()
    {
        if(_inputHandler == null) return;
        _inputHandler.OnToggleInventory -= OnToggleInventory;
    }

    private void OnToggleInventory()
        => Toggle(inventoryScreen);

    private void Toggle(GameObject screen)
    {
        if (screen == null)
        {
            Debug.LogWarning($"[PlayerMenuNavigate] Screen reference is null");
            return;
        }
        
        Debug.Log($"[PlayerMenuNavigate] Toggle screen: {screen.name}]");
        if (_currentScreen == screen)
        {
            Close();
            return;
        }
        
        if (_currentScreen != null)
        {
            _currentScreen.SetActive(false);
        }
        
        screen.SetActive(true);
        _currentScreen = screen;
    }

    public void Open()
    {
        if(_currentScreen == null) return;
        _currentScreen.SetActive(true);
    }

    public void Close()
    {
        if(_currentScreen == null) return;
        _currentScreen.SetActive(false);
        _currentScreen = null;
    }
}