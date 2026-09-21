using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
    
public class MainMenu : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera overviewCamera;
    [SerializeField] private CinemachineCamera playerFocusCamera;
    [SerializeField] private CinemachineCamera changeSkinCamera;
    
    [Header("UI")]
    [SerializeField] private MainMenuScreen screen;

    private const int InactivePriority = 0;
    private const int ActivePriority = 20;

    private void OnEnable()
    {
        screen.StartGameClicked += StartGame;
        screen.OptionClicked += Option;
        screen.QuitClicked += QuitGame;
        screen.ChangeCosmeticClicked += HandleChangeCosmetic;
        screen.CloseCosmeticClicked += HandleCloseCosmetic;
    }

    private void OnDisable()
    {
        screen.StartGameClicked -= StartGame;
        screen.OptionClicked -= Option;
        screen.QuitClicked -= QuitGame;
        screen.ChangeCosmeticClicked -= HandleChangeCosmetic;
        screen.CloseCosmeticClicked -= HandleCloseCosmetic;
    }

    public void OpenMainMenu()
    {
        StartCoroutine(FocusToPlayer());
    }
    
    private IEnumerator FocusToPlayer()
    {
        SetPriority(playerFocusCamera, ActivePriority);
        SetPriority(overviewCamera, InactivePriority);
        yield return WaitForBlend();
        screen.Show();
    }

    private void HandleChangeCosmetic()
    {
        SetPriority(changeSkinCamera, ActivePriority);
        SetPriority(playerFocusCamera, InactivePriority);
        // TODO: Implement change skin system
    }

    private void HandleCloseCosmetic()
    {
        SetPriority(playerFocusCamera, ActivePriority);
        SetPriority(changeSkinCamera, InactivePriority);
    }
    
    private void StartGame()
    {
        _ = SceneLoader.Instance.LoadScene("Lobby", false);
    }
    
    private void Option()
    {
        // TODO: Open option menu - Not yet right now
        Debug.Log($"[MainMenu] Open Options!");
    }
    
    private void QuitGame()
    {
        Debug.Log("[MainMenu] Quit Game!");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;        
#endif
    }
    
    private WaitUntil WaitForBlend()
    {
        return new WaitUntil(() => brain == null || !brain.IsBlending);
    }

    private void SetPriority(CinemachineCamera cam, int priority)
    {
        if(cam == null) return;
        cam.Priority = new PrioritySettings { Enabled = true, Value = priority };
    }
}