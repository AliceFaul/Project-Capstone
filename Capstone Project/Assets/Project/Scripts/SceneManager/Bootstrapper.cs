using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static async void Initialize()
    {
        try
        {
            Debug.Log("[Bootstrapper] Initializing...");
            
            if(SceneManager.GetActiveScene().name != "Bootstrap")
                await SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private async void Awake()
    {
        try
        {
            await sceneLoader.LoadScene("MainMenu", true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Bootstrapper] Failed: {e.Message}\n{e.StackTrace}");
        }
    }
}