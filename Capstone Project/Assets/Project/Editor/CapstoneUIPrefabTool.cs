#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// Editor only. Reuses the project's existing views/controllers.
public sealed class CapstoneUIPrefabTool : EditorWindow
{
    [SerializeField] private string iconFolder = "Assets/CapstoneUI/Icons";
    [SerializeField] private string outputFolder = "Assets/CapstoneUI/Generated";
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite gemIcon;
    [SerializeField] private RenderTexture characterPreview;
    [SerializeField] private PlayerHUDController existingHUD;
    [SerializeField] private int healAbilityIndex;
    [SerializeField] private int rollAbilityIndex = 1;
    [SerializeField] private bool buildHUD = true;
    [SerializeField] private bool buildInventory = true;
    [SerializeField] private bool normalizeIconImport = true;
    private Vector2 scroll;
    private string resultMessage;

    [MenuItem("Tools/UI/Capstone Prefab Tool")]
    public static void Open() => GetWindow<CapstoneUIPrefabTool>("Capstone UI");

    public static void OpenInventory()
    {
        var window = GetWindow<CapstoneUIPrefabTool>("Capstone UI");
        window.buildHUD = false;
        window.buildInventory = true;
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("HUD / Inventory — approved layout", EditorStyles.boldLabel);
        iconFolder = EditorGUILayout.TextField("Icon folder", iconFolder);
        outputFolder = EditorGUILayout.TextField("Output folder", outputFolder);
        font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font (optional)", font, typeof(TMP_FontAsset), false);
        goldIcon = (Sprite)EditorGUILayout.ObjectField("Gold icon", goldIcon, typeof(Sprite), false);
        gemIcon = (Sprite)EditorGUILayout.ObjectField("Gem icon", gemIcon, typeof(Sprite), false);
        characterPreview = (RenderTexture)EditorGUILayout.ObjectField("Character RenderTexture", characterPreview, typeof(RenderTexture), false);
        EditorGUILayout.Space();
        existingHUD = (PlayerHUDController)EditorGUILayout.ObjectField("Copy HUD asset settings", existingHUD, typeof(PlayerHUDController), true);
        EditorGUILayout.HelpBox("Copies cooldown channel and ready sounds only. Player references are resolved at runtime. Match Heal/Roll indices to PlayerAbilityHolder.Abilities.", MessageType.Info);
        healAbilityIndex = EditorGUILayout.IntField("Heal ability index", healAbilityIndex);
        rollAbilityIndex = EditorGUILayout.IntField("Roll ability index", rollAbilityIndex);
        EditorGUILayout.Space();
        buildHUD = EditorGUILayout.Toggle("Build HUD", buildHUD);
        buildInventory = EditorGUILayout.Toggle("Build Inventory", buildInventory);
        normalizeIconImport = EditorGUILayout.Toggle("Set icon import settings", normalizeIconImport);
        EditorGUILayout.HelpBox("Every build creates a new folder. Inventory includes a wired RuneSocketPopup. Opening Inventory, saving and gameplay input remain with your existing game flow.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            if (GUILayout.Button("Validate inputs")) Run(false);
            if (GUILayout.Button("Create prefabs", GUILayout.Height(34))) Run(true);
        }
        if (!string.IsNullOrEmpty(resultMessage)) EditorGUILayout.HelpBox(resultMessage, MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    private void Run(bool create)
    {
        try
        {
            if (!buildHUD && !buildInventory) throw new InvalidOperationException("Select at least one output.");
            var options = new CapstoneUIBuildOptions
            {
                IconFolder = iconFolder.TrimEnd('/'), Font = font != null ? font : TMP_Settings.defaultFontAsset,
                Gold = goldIcon, Gem = gemIcon, CharacterPreview = characterPreview,
                ExistingHUD = existingHUD, HealIndex = healAbilityIndex, RollIndex = rollAbilityIndex
            };
            CapstoneUIEditor.ValidateInputs(options, outputFolder, buildHUD, buildInventory);
            if (!normalizeIconImport) CapstoneUIEditor.ResolveIcons(options);
            if (!create) { resultMessage = "Inputs and component contracts are valid."; return; }
            if (normalizeIconImport) CapstoneUIEditor.ConfigureIconImports(options);
            CapstoneUIEditor.ResolveIcons(options);
            string parent = outputFolder.TrimEnd('/');
            Directory.CreateDirectory(parent);
            AssetDatabase.Refresh();
            options.Output = AssetDatabase.GenerateUniqueAssetPath(parent + "/Build_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(options.Output);
            AssetDatabase.Refresh();
            CapstoneUIEditor.CreateShapes(options);
            if (buildHUD) PlayerHUDPrefabBuilder.Build(options);
            if (buildInventory) InventoryUIBuilder.BuildToFolder(options);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(options.Output);
            EditorGUIUtility.PingObject(Selection.activeObject);
            resultMessage = "Created: " + options.Output + "\nOpen each prefab to inspect the result; drag HUD/InventoryCanvas into your gameplay scene when ready.";
            Debug.Log("[Capstone UI] " + resultMessage);
        }
        catch (Exception ex)
        {
            resultMessage = ex.Message;
            Debug.LogException(ex);
        }
    }
}
#endif
