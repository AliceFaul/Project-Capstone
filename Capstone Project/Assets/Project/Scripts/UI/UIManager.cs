using System;
using System.Threading.Tasks;
using UnityEngine;

public class UIManager : MonoBehaviour, IManager
{
    public static UIManager Instance { get; private set; }
    
    private IPopupService _popupService;
    
    public async Task<bool> Initialize()
    {
        _popupService = new PopupService();
        // TODO: Add floating text service
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
}