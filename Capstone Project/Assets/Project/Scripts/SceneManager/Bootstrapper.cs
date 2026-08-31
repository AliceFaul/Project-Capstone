using System;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;
    
    private async void Awake()
    {
        try
        {
            Debug.Log("Bootstrapper...");
            await sceneLoader.LoadScene("MainMenu", true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Bootstrapper] Failed: {e.Message}\n{e.StackTrace}");
        }
    }
}