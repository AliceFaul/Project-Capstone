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
    [SerializeField] private bool buildShop;
    [SerializeField] private bool buildLevelSelect;
    [SerializeField] private Sprite levelMap;
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
        window.buildShop = false;
        window.buildLevelSelect = false;
    }

    public static void OpenShop()
    {
        var window = GetWindow<CapstoneUIPrefabTool>("Capstone UI");
        window.buildHUD = false;
        window.buildInventory = false;
        window.buildShop = true;
        window.buildLevelSelect = false;
    }

    public static void OpenLevelSelect()
    {
        var window = GetWindow<CapstoneUIPrefabTool>("Capstone UI");
        window.buildHUD = false;
        window.buildInventory = false;
        window.buildShop = false;
        window.buildLevelSelect = true;
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("HUD / Inventory / Shop / Level Select", EditorStyles.boldLabel);
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
        buildShop = EditorGUILayout.Toggle("Build Shop", buildShop);
        buildLevelSelect = EditorGUILayout.Toggle("Build Level Select", buildLevelSelect);
        if (buildLevelSelect)
        {
            levelMap = (Sprite)EditorGUILayout.ObjectField("Level map (optional)", levelMap, typeof(Sprite), false);
            EditorGUILayout.HelpBox("Map markers keep the existing right-side coordinate region. Level previews and rewards still come from your level data.", MessageType.Info);
        }
        normalizeIconImport = EditorGUILayout.Toggle("Set icon import settings", normalizeIconImport);
        EditorGUILayout.HelpBox("Every build creates a new folder. All screens share the configured font and icon library. Add the generated canvas to your scene after inspecting it.", MessageType.Info);
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
            if (!buildHUD && !buildInventory && !buildShop && !buildLevelSelect) throw new InvalidOperationException("Select at least one output.");
            var options = new CapstoneUIBuildOptions
            {
                IconFolder = iconFolder.TrimEnd('/'), Font = font != null ? font : TMP_Settings.defaultFontAsset,
                Gold = goldIcon, Gem = gemIcon, CharacterPreview = characterPreview,
                ExistingHUD = existingHUD, HealIndex = healAbilityIndex, RollIndex = rollAbilityIndex
            };
            CapstoneUIEditor.ValidateInputs(options, outputFolder, buildHUD, buildInventory);
            if (buildShop) ShopUIBuilder.ValidateInputs();
            if (buildLevelSelect) LevelSelectUIBuilder.ValidateInputs();
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
            if (buildShop) ShopUIBuilder.BuildToFolder(options);
            if (buildLevelSelect) LevelSelectUIBuilder.BuildToFolder(options, levelMap);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(options.Output);
            EditorGUIUtility.PingObject(Selection.activeObject);
            resultMessage = "Created: " + options.Output + "\nOpen each prefab to inspect the result, then add the required canvas to your scene.";
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