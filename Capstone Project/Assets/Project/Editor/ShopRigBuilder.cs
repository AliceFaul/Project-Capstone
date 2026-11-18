using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class ShopRigBuilder
{
    private const string BlurShaderPath = "Assets/Project/Shaders/UIBlurBackdrop.shader";

    [MenuItem("Tools/Shop/Create Shop Rig")]
    public static void Create()
    {
        const string dir = "Assets/Shop";
        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        var root = new GameObject("ShopRig");
        Undo.RegisterCreatedObjectUndo(root, "Create shop rig");
        var director = root.AddComponent<ShopSceneDirector>();

        // virtual camera (inactive until the shop opens)
        var cameraObject = new GameObject("ShopCamera");
        cameraObject.transform.SetParent(root.transform, false);
        var cameraType = Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine")
                         ?? Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
        if (cameraType != null) ObjectFactory.AddComponent(cameraObject, cameraType);
        else Debug.LogWarning("[Shop] Cinemachine was not found - add a virtual camera component to 'ShopCamera' yourself.");
        cameraObject.SetActive(false);

        // blur volume
        var volumeObject = new GameObject("FocusVolume");
        volumeObject.transform.SetParent(root.transform, false);
        var volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        volume.weight = 0f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        string path = AssetDatabase.GenerateUniqueAssetPath(dir + "/ShopFocusProfile.asset");
        AssetDatabase.CreateAsset(profile, path);

        var dof = profile.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(10f);
        dof.gaussianEnd.Override(25f);
        dof.gaussianMaxRadius.Override(1.5f);
        AssetDatabase.AddObjectToAsset(dof, profile);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.5f);
        AssetDatabase.AddObjectToAsset(vignette, profile);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        volume.sharedProfile = profile;

        var so = new SerializedObject(director);
        so.FindProperty("shopCamera").objectReferenceValue = cameraObject;
        so.FindProperty("focusVolume").objectReferenceValue = volume;
        so.ApplyModifiedPropertiesWithoutUndo();

        AddBackdrop(director);

        Selection.activeGameObject = root;
        Debug.Log("[Shop] ShopRig created. Next: pose ShopCamera (Scene view > GameObject > Align With View), assign the Merchant on the director.");
    }

    [MenuItem("Tools/Shop/Add Backdrop To Shop Rig")]
    public static void AddBackdropToSelected()
    {
        var director = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<ShopSceneDirector>() : null;
        if (director == null) director = UnityEngine.Object.FindFirstObjectByType<ShopSceneDirector>();
        if (director == null)
        {
            Debug.LogWarning("[Shop] No ShopSceneDirector found - create the shop rig first.");
            return;
        }

        AddBackdrop(director);
        Selection.activeGameObject = director.gameObject;
    }

    // Overlay canvas under the shop UI: blurred snapshot of the left side (fading out toward the merchant) + a dark gradient.
    public static void AddBackdrop(ShopSceneDirector director)
    {
        var canvasObject = new GameObject("ShopBackdropCanvas", typeof(Canvas));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create shop backdrop");
        canvasObject.transform.SetParent(director.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40; // above the game, below the shop UI

        var blurRect = NewFullScreen("BackdropBlur", canvasObject.transform);
        var raw = blurRect.gameObject.AddComponent<RawImage>();
        raw.raycastTarget = true; // also stops clicks from reaching the world
        raw.enabled = false;      // shown after the first capture

        var dimRect = NewFullScreen("BackdropDim", canvasObject.transform);
        var dim = dimRect.gameObject.AddComponent<RawImage>();
        dim.raycastTarget = false;
        dimRect.gameObject.AddComponent<ShopBackdropGradient>();

        var blur = canvasObject.AddComponent<UIBlurBackdrop>();
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(BlurShaderPath);
        if (shader == null) Debug.LogWarning($"[Shop] Blur shader not found at {BlurShaderPath} - the left side will not be blurred.");

        var blurSo = new SerializedObject(blur);
        blurSo.FindProperty("blurImage").objectReferenceValue = raw;
        blurSo.FindProperty("blurShader").objectReferenceValue = shader;
        blurSo.FindProperty("fadeRight").boolValue = true;
        blurSo.FindProperty("fadeStart").floatValue = 0.55f;
        blurSo.FindProperty("fadeEnd").floatValue = 0.78f;
        blurSo.ApplyModifiedPropertiesWithoutUndo();

        var directorSo = new SerializedObject(director);
        directorSo.FindProperty("backdropRoot").objectReferenceValue = canvasObject;
        directorSo.FindProperty("backdropBlur").objectReferenceValue = blur;
        directorSo.ApplyModifiedPropertiesWithoutUndo();

        canvasObject.SetActive(false);
        Debug.Log("[Shop] Backdrop added to the shop rig.");
    }

    private static RectTransform NewFullScreen(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
}