#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ShopRigBuilder
{
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

        Selection.activeGameObject = root;
        Debug.Log("[Shop] ShopRig created. Next: pose ShopCamera (Scene view > GameObject > Align With View), assign the Merchant on the director.");
    }
}
#endif