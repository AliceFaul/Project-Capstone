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
    [SerializeField] private bool buildSettings;
    [SerializeField] private bool buildPauseMenu;
    [SerializeField] private bool buildNodeMap;
    [SerializeField] private ControlsConfig controlsConfig;
    [SerializeField] private NodeMapStyle nodeMapStyle;
    [SerializeField] private LevelCatalog pauseCatalog;
    [SerializeField] private CombatPowerConfig pausePowerConfig;
    [SerializeField] private Shader pauseBlurShader;
    [SerializeField] private Sprite levelMap;
    [SerializeField] private bool normalizeIconImport = true;
    private Vector2 scroll;
    private string resultMessage;

    [MenuItem("Tools/UI/Capstone Prefab Tool")]
    public static void Open() => GetWindow<CapstoneUIPrefabTool>("Capstone UI");

    private enum ScreenChoice { Inventory, Shop, LevelSelect, Settings, PauseMenu, NodeMap, ThreeMenus }

    public static void OpenInventory() => OpenWith(ScreenChoice.Inventory);
    public static void OpenShop() => OpenWith(ScreenChoice.Shop);
    public static void OpenLevelSelect() => OpenWith(ScreenChoice.LevelSelect);
    public static void OpenSettings() => OpenWith(ScreenChoice.Settings);
    public static void OpenPauseMenu() => OpenWith(ScreenChoice.PauseMenu);
    public static void OpenNodeMap() => OpenWith(ScreenChoice.NodeMap);

    [MenuItem("Tools/UI/Build Settings, Pause and Node Map")]
    private static void OpenThreeMenus() => OpenWith(ScreenChoice.ThreeMenus);

    private static void OpenWith(ScreenChoice choice)
    {
        var window = GetWindow<CapstoneUIPrefabTool>("Capstone UI");
        bool threeMenus = choice == ScreenChoice.ThreeMenus;
        window.buildHUD = false;
        window.buildInventory = choice == ScreenChoice.Inventory;
        window.buildShop = choice == ScreenChoice.Shop;
        window.buildLevelSelect = choice == ScreenChoice.LevelSelect;
        window.buildSettings = threeMenus || choice == ScreenChoice.Settings;
        window.buildPauseMenu = threeMenus || choice == ScreenChoice.PauseMenu;
        window.buildNodeMap = threeMenus || choice == ScreenChoice.NodeMap;
        window.resultMessage = null;
        window.Repaint();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Screens", EditorStyles.boldLabel);
        buildHUD = EditorGUILayout.Toggle("Build HUD", buildHUD);
        buildInventory = EditorGUILayout.Toggle("Build Inventory", buildInventory);
        buildShop = EditorGUILayout.Toggle("Build Shop", buildShop);
        buildLevelSelect = EditorGUILayout.Toggle("Build Level Select", buildLevelSelect);
        buildSettings = EditorGUILayout.Toggle("Build Settings", buildSettings);
        buildPauseMenu = EditorGUILayout.Toggle("Build Pause Menu", buildPauseMenu);
        buildNodeMap = EditorGUILayout.Toggle("Build Node Map", buildNodeMap);

        EditorGUILayout.Space();
        iconFolder = EditorGUILayout.TextField("Icon folder", iconFolder);
        outputFolder = EditorGUILayout.TextField("Output folder", outputFolder);
        font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font (optional)", font, typeof(TMP_FontAsset), false);
        normalizeIconImport = EditorGUILayout.Toggle("Set icon import settings", normalizeIconImport);
        if (buildHUD || buildInventory || buildShop || buildPauseMenu)
        {
            goldIcon = (Sprite)EditorGUILayout.ObjectField("Gold icon", goldIcon, typeof(Sprite), false);
            gemIcon = (Sprite)EditorGUILayout.ObjectField("Gem icon", gemIcon, typeof(Sprite), false);
        }
        if (buildHUD || buildInventory || buildShop)
            characterPreview = (RenderTexture)EditorGUILayout.ObjectField("Character RenderTexture", characterPreview, typeof(RenderTexture), false);
        if (buildHUD)
        {
            existingHUD = (PlayerHUDController)EditorGUILayout.ObjectField("Copy HUD asset settings", existingHUD, typeof(PlayerHUDController), true);
            healAbilityIndex = EditorGUILayout.IntField("Heal ability index", healAbilityIndex);
            rollAbilityIndex = EditorGUILayout.IntField("Roll ability index", rollAbilityIndex);
            EditorGUILayout.HelpBox("Match Heal and Roll indices to PlayerAbilityHolder.Abilities.", MessageType.Info);
        }
        if (buildLevelSelect)
            levelMap = (Sprite)EditorGUILayout.ObjectField("Level map (optional)", levelMap, typeof(Sprite), false);
        if (buildSettings || buildNodeMap)
        {
            controlsConfig = (ControlsConfig)EditorGUILayout.ObjectField("Controls config", controlsConfig, typeof(ControlsConfig), false);
            EditorGUILayout.HelpBox("When empty, both screens use Assets/Settings/ControlsConfig.asset.", MessageType.Info);
        }
        if (buildNodeMap)
            nodeMapStyle = (NodeMapStyle)EditorGUILayout.ObjectField("Node map style", nodeMapStyle, typeof(NodeMapStyle), false);
        if (buildPauseMenu)
        {
            pauseCatalog = (LevelCatalog)EditorGUILayout.ObjectField("Pause level catalog", pauseCatalog, typeof(LevelCatalog), false);
            pausePowerConfig = (CombatPowerConfig)EditorGUILayout.ObjectField("Pause power config", pausePowerConfig, typeof(CombatPowerConfig), false);
            pauseBlurShader = (Shader)EditorGUILayout.ObjectField("Pause blur shader", pauseBlurShader, typeof(Shader), false);
        }
        EditorGUILayout.Space();
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
            if (!buildHUD && !buildInventory && !buildShop && !buildLevelSelect && !buildSettings && !buildPauseMenu && !buildNodeMap)
                throw new InvalidOperationException("Select at least one output.");
            var options = new CapstoneUIBuildOptions
            {
                IconFolder = iconFolder.TrimEnd('/'), Font = font != null ? font : TMP_Settings.defaultFontAsset,
                Gold = goldIcon, Gem = gemIcon, CharacterPreview = characterPreview,
                ExistingHUD = existingHUD, HealIndex = healAbilityIndex, RollIndex = rollAbilityIndex
            };
            CapstoneUIEditor.ValidateInputs(options, outputFolder, buildHUD, buildInventory);
            if (buildShop) ShopUIBuilder.ValidateInputs();
            if (buildLevelSelect) LevelSelectUIBuilder.ValidateInputs();
            if (buildSettings) SettingsUIBuilder.ValidateInputs(controlsConfig);
            if (buildPauseMenu) PauseMenuBuilder.ValidateInputs();
            if (buildNodeMap) NodeMapUIBuilder.ValidateInputs(nodeMapStyle, controlsConfig);
            if (!normalizeIconImport && !create) CapstoneUIEditor.ResolveIcons(options);
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
            if (buildSettings) SettingsUIBuilder.BuildToFolder(options, controlsConfig);
            if (buildPauseMenu) PauseMenuBuilder.BuildToFolder(options, pauseCatalog, pausePowerConfig, pauseBlurShader);
            if (buildNodeMap) NodeMapUIBuilder.BuildToFolder(options, nodeMapStyle, controlsConfig);
            if (buildSettings || buildPauseMenu || buildNodeMap) InventoryLocalizationTool.AddKeys();
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