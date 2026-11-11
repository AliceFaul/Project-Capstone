using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShopSceneDirector : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("The shop virtual camera. Keep this GameObject INACTIVE; it is enabled while the shop is open. Give it a higher " +
             "priority than the gameplay camera (or at least activate it later - ties go to the newest).")]
    [SerializeField] private GameObject shopCamera;
    
    [Tooltip("Used when no CinemachineBrain is found: seconds to wait for the camera blend.")]
    [SerializeField] private float blendWaitFallback = 0.9f;
    [SerializeField] private float maxBlendWait = 3f;

    [Header("Background blur (URP volume with Depth Of Field)")]
    [SerializeField] private Volume focusVolume;
    
    [Tooltip("The merchant: everything farther than this (plus the offsets below) is blurred.")]
    [SerializeField] private Transform merchant;
    [SerializeField] private float blurStartBehindMerchant = 2f;
    [SerializeField] private float blurEndBehindMerchant = 12f;
    [Range(0.5f, 1.5f)] [SerializeField] private float blurMaxRadius = 1.5f;
    [SerializeField] private float focusFadeTime = 0.6f;

    [Header("Player")]
    [SerializeField] private bool hidePlayer = true;
    
    [Tooltip("Turn off once the shop UI handles Esc itself.")]
    [SerializeField] private bool closeWithEscape = true;

    public bool IsOpen { get; private set; }
    public ShopService Shop { get; private set; }

    public event Action<ShopService> Opened; // the shop UI shows itself here
    public event Action Closed;              // ...and hides here

    private PlayerController _player;
    private readonly List<Renderer> _hidden = new List<Renderer>();
    private bool _changed;
    private Coroutine _focusRoutine;

    public void Open(ShopService shop, PlayerController player)
    {
        if (IsOpen || shop == null || player == null) return;

        IsOpen = true;
        Shop = shop;
        _player = player;
        _changed = false;
        shop.Changed += OnShopChanged;
        StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (Shop != null) Shop.Changed -= OnShopChanged;
        Closed?.Invoke();
        StartCoroutine(CloseRoutine());
    }

    private void Update()
    {
        if (IsOpen && closeWithEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    private void OnShopChanged() => _changed = true;

    private IEnumerator OpenRoutine()
    {
        _player.CmdCombatLocked(true);
        if (hidePlayer) SetPlayerVisible(false);

        PrepareFocus();
        if (shopCamera != null) shopCamera.SetActive(true);
        StartFocus(1f);

        yield return WaitForCameraBlend();
        if (!IsOpen) yield break;

        Opened?.Invoke(Shop);
        if (Opened == null) LogStock(Shop); // no UI yet: at least show the stock in the console
    }

    private IEnumerator CloseRoutine()
    {
        if (shopCamera != null) shopCamera.SetActive(false);
        StartFocus(0f);

        yield return WaitForCameraBlend();

        SetPlayerVisible(true);
        if (_player != null) _player.CmdCombatLocked(false);
        if (_changed) SaveHelper.SaveNow();
    }

    private void PrepareFocus()
    {
        if (focusVolume == null) return;

        Vector3 cameraPosition = shopCamera != null ? shopCamera.transform.position : (Camera.main != null ? Camera.main.transform.position : transform.position);
        float distance = merchant != null ? Vector3.Distance(cameraPosition, merchant.position) : 8f;

        if (focusVolume.profile.TryGet(out DepthOfField dof))
        {
            dof.active = true;
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(distance + blurStartBehindMerchant);
            dof.gaussianEnd.Override(distance + blurEndBehindMerchant);
            dof.gaussianMaxRadius.Override(blurMaxRadius);
        }
        else
        {
            Debug.LogWarning("[ShopSceneDirector] The focus volume profile has no Depth Of Field override.", focusVolume);
        }
    }

    private void StartFocus(float target)
    {
        if (focusVolume == null) return;
        if (_focusRoutine != null) StopCoroutine(_focusRoutine);
        _focusRoutine = StartCoroutine(FadeFocus(target));
    }

    private IEnumerator FadeFocus(float target)
    {
        float start = focusVolume.weight;
        for (float t = 0f; t < focusFadeTime; t += Time.unscaledDeltaTime)
        {
            focusVolume.weight = Mathf.Lerp(start, target, t / focusFadeTime);
            yield return null;
        }
        focusVolume.weight = target;
    }

    private IEnumerator WaitForCameraBlend()
    {
        yield return null;
        yield return null; // the brain starts the blend in its LateUpdate

        var brain = Camera.main != null ? Camera.main.GetComponent("CinemachineBrain") : null;
        float waited = 0f;

        if (brain == null)
        {
            yield return new WaitForSecondsRealtime(blendWaitFallback);
            yield break;
        }

        while (IsBlending(brain) && waited < maxBlendWait)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static bool IsBlending(Component brain)
    {
        var property = brain.GetType().GetProperty("IsBlending");
        return property != null && property.PropertyType == typeof(bool) && (bool)property.GetValue(brain);
    }

    private void SetPlayerVisible(bool visible)
    {
        if (!visible)
        {
            _hidden.Clear();
            if (_player == null) return;

            foreach (var renderer in _player.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                renderer.enabled = false;
                _hidden.Add(renderer);
            }
            return;
        }

        foreach (var renderer in _hidden)
        {
            if (renderer != null) renderer.enabled = true;
        }
        _hidden.Clear();
    }

    private static void LogStock(ShopService shop)
    {
        foreach (var offer in shop.Offers)
            Debug.Log($"[Shop] {offer.Definition.itemName} ({offer.Definition.rarity}) - {offer.Price} gold");
    }
}

public static class SaveHelper
{
    public static async void SaveNow()
    {
        try
        {
            var startup = StartupProcessor.Instance;
            if (startup == null || !startup.GetService(out SaveService save)) return;
            await save.SaveData();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}