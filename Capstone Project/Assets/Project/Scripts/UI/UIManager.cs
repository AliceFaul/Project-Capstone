using System;
using System.Threading.Tasks;
using UnityEngine;

public class UIManager : MonoBehaviour, IManager
{
    public static UIManager Instance { get; private set; }
    
    private IPopupService _popupService;
    private IFloatingTextService _floatingTextService;
    
    public async Task<bool> Initialize()
    {
        _popupService = new PopupService();
        _floatingTextService = new FloatingTextService();
        await Task.CompletedTask;
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public IPopupService GetPopupService() => _popupService;
    public IFloatingTextService GetFloatingTextService() => _floatingTextService;
}