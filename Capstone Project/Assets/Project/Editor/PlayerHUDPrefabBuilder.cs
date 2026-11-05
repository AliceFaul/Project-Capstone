#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CapstoneUIEditor;

public static class PlayerHUDPrefabBuilder
{
    // Size of the HUD group only; CanvasScaler and other screens keep their own scale.
    private const float DefaultHUDScale = .75f;

    [MenuItem("Tools/UI/Build Player HUD")]
    private static void OpenTool() => CapstoneUIPrefabTool.Open();

    [MenuItem("Tools/UI/HUD Scale/Small (0.70)")]
    private static void SmallHUD() => ResizeSelectedHUD(.70f);

    [MenuItem("Tools/UI/HUD Scale/Compact (0.75)")]
    private static void CompactHUD() => ResizeSelectedHUD(DefaultHUDScale);

    [MenuItem("Tools/UI/HUD Scale/Medium (0.80)")]
    private static void MediumHUD() => ResizeSelectedHUD(.80f);

    [MenuItem("Tools/UI/HUD Scale/Original (1.00)")]
    private static void OriginalHUD() => ResizeSelectedHUD(1f);

    private static void ResizeSelectedHUD(float scale)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Capstone UI] Exit Play Mode before resizing the HUD.");
            return;
        }
        var selected = Selection.activeGameObject;
        if (selected == null || EditorUtility.IsPersistent(selected))
        {
            Debug.LogWarning("[Capstone UI] Select PlayerHUD (or its child) in the scene or open its prefab in Prefab Mode first.");
            return;
        }
        var view = selected.GetComponentInParent<PlayerHUDView>(true)
                   ?? selected.GetComponentInChildren<PlayerHUDView>(true);
        var hud = view != null ? view.transform.Find("HUD") as RectTransform : null;
        if (hud == null)
        {
            Debug.LogWarning("[Capstone UI] Expected the generated PlayerHUDView root with a direct child named HUD.");
            return;
        }
        Undo.RecordObject(hud, "Resize Player HUD");
        // Absolute size: applying the same preset again does not shrink it further.
        hud.localScale = new Vector3(scale, scale, hud.localScale.z);
        if (PrefabUtility.IsPartOfPrefabInstance(hud))
            PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
        EditorUtility.SetDirty(hud);
        Debug.Log("[Capstone UI] HUD scale set to " + scale.ToString("0.00") + ". Save the scene/prefab to keep this change. Undo is available.", hud);
    }

    public static GameObject Build(CapstoneUIBuildOptions o)
    {
        using (var scene = new BuildScene())
        {
            var root = Canvas("PlayerHUD", 10);
            scene.Own(root);
            var view = root.AddComponent<PlayerHUDView>();
            var controller = root.AddComponent<PlayerHUDController>();
            Ref(controller, "view", view);
            CopyAssetSettings(o.ExistingHUD, controller);

            var hud = Rect("HUD", root.transform, 0, 0, 1400, 330);
            hud.anchorMin = hud.anchorMax = new Vector2(.5f, 0);
            hud.pivot = new Vector2(.5f, 0);
            hud.anchoredPosition = new Vector2(0, 24);
            hud.localScale = new Vector3(DefaultHUDScale, DefaultHUDScale, 1f);
            var panel = Image("Base", hud, 0, 126, 1400, 188, Color.white, o.Shape("Panel"));
            panel.type = UnityEngine.UI.Image.Type.Sliced;

            Icon("Inventory", hud, 78, 15, 108, 108, o.Icon("UI_Backpack"));
            Key(hud, 158, 18, 52, 50, "B", o);
            Icon("Map", hud, 1180, 20, 112, 104, o.Icon("HUD_Map"));
            Key(hud, 1268, 18, 92, 50, "Tab", o);

            Image("HeartBackground", hud, 242, 60, 242, 242, Color.white, o.Shape("HeartBackground"));
            var heart = Image("HeartFill", hud, 242, 60, 242, 242, new Color(.95f,.055f,.12f), o.Shape("HeartFill"));
            heart.type = UnityEngine.UI.Image.Type.Filled;
            heart.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
            heart.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
            heart.fillAmount = 1;
            Image("HeartFrame", hud, 242, 60, 242, 242, Color.white, o.Shape("HeartFrame"));
            Ref(view, "heartFill", heart);

            var so = new SerializedObject(view);
            var artifacts = so.FindProperty("artifactSlots");
            artifacts.arraySize = 3;
            for (int i = 0; i < 3; i++)
                Slot(artifacts.GetArrayElementAtIndex(i), hud, "Artifact" + (i + 1), 536 + i * 200, 70, 174, 174, o.Icon("UI_Artifact"), (i + 1).ToString(), .28f, o);
            var abilities = so.FindProperty("abilitySlots");
            abilities.arraySize = Math.Max(o.HealIndex, o.RollIndex) + 1;
            Slot(abilities.GetArrayElementAtIndex(o.HealIndex), hud, "Heal", 76, 160, 112, 112, o.Icon("HUD_Heal"), "E", 1, o);
            Slot(abilities.GetArrayElementAtIndex(o.RollIndex), hud, "Roll", 1210, 160, 126, 112, o.Icon("HUD_Roll"), "R", 1, o);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Arrow and count are siblings of the regeneration root, so they stay visible at max ammo.
            Icon("Arrow", hud, 781, 7, 48, 48, o.Icon("HUD_Arrow"));
            var ammo = Text("ArrowCount", hud, 830, 7, 70, 48, "15", 34, Ivory, o);
            Ref(view, "ammoText", ammo);
            var regen = Rect("AmmoRegeneration", hud, 0, 0, 1400, 70);
            var left = RefillTrack("Left", regen, 536, 28, 205, false, o);
            var right = RefillTrack("Right", regen, 905, 28, 205, true, o);
            Ref(view, "ammoRegenRoot", regen.gameObject);
            Ref(view, "ammoRegenFill", left);
            Ref(view, "ammoRegenRightFill", right);

            Text("LevelLabel", hud, 496, 258, 52, 44, "Lv", 30, Ivory, o);
            var level = Text("Level", hud, 548, 258, 52, 44, "1", 34, new Color(.83f,.44f,.98f), o);
            Ref(view, "levelText", level);
            var expRoot = Rect("Experience", hud, 604, 272, 506, 18);
            var slider = expRoot.gameObject.AddComponent<Slider>();
            var track = Image("Track", expRoot, 0, 0, 506, 18, new Color(.29f,.30f,.31f));
            Stretch(track.rectTransform);
            var fill = Image("Fill", expRoot, 0, 0, 506, 18, new Color(.69f,.20f,.98f));
            Stretch(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            slider.minValue = 0; slider.maxValue = 1; slider.value = 0;
            slider.interactable = false; slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            Ref(view, "expSlider", slider);

            Validate(view);
            return Save(root, o.Output + "/PlayerHUD.prefab");
        }
    }

    private static void Slot(SerializedProperty slot, Transform parent, string name, float x, float y, float w, float h, Sprite placeholder, string key, float placeholderAlpha, CapstoneUIBuildOptions o)
    {
        var frame = Image(name, parent, x, y, w, h, Color.white, o.Shape("Slot"));
        frame.type = UnityEngine.UI.Image.Type.Sliced;
        var empty = Icon("Placeholder", frame.transform, 5, 5, w - 10, h - 10, placeholder, placeholderAlpha);
        var item = Icon("Icon", frame.transform, 5, 5, w - 10, h - 10, null);
        item.enabled = false;
        var cooldown = Image("Cooldown", frame.transform, 5, 5, w - 10, h - 10, new Color(0,0,0,.68f), o.Shape("White"));
        cooldown.type = UnityEngine.UI.Image.Type.Filled;
        cooldown.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
        cooldown.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
        cooldown.fillAmount = 0; cooldown.enabled = false;
        Key(frame.transform, w - 37, h - 46, 58, 54, key, o);
        slot.FindPropertyRelative("icon").objectReferenceValue = item;
        slot.FindPropertyRelative("placeholder").objectReferenceValue = empty;
        slot.FindPropertyRelative("cooldownOverlay").objectReferenceValue = cooldown;
    }
    private static void Key(Transform parent, float x, float y, float w, float h, string value, CapstoneUIBuildOptions o)
    {
        var key = Image("Key_" + value, parent, x, y, w, h, Color.white, o.Shape("Key"));
        key.type = UnityEngine.UI.Image.Type.Sliced;
        Text("Label", key.transform, 2, 2, w - 4, h - 4, value, value.Length > 1 ? 27 : 36, new Color(.055f,.06f,.06f), o, TextAlignmentOptions.Center);
    }
    private static Image RefillTrack(string name, Transform parent, float x, float y, float w, bool fromRight, CapstoneUIBuildOptions o)
    {
        var track = Image(name + "Track", parent, x, y, w, 15, new Color(.20f,.21f,.21f));
        var fill = Image("Fill", track.transform, 2, 2, w - 4, 11, Ivory, o.Shape("White"));
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillOrigin = fromRight ? (int)UnityEngine.UI.Image.OriginHorizontal.Right : (int)UnityEngine.UI.Image.OriginHorizontal.Left;
        fill.fillAmount = 0;
        return fill;
    }
    private static void CopyAssetSettings(PlayerHUDController source, PlayerHUDController destination)
    {
        if (source == null) return;
        var input = new SerializedObject(source);
        var output = new SerializedObject(destination);
        foreach (string name in new[] { "cooldownChannel", "abilityReadyCue", "artifactReadyCue" })
        {
            var a = input.FindProperty(name); var b = output.FindProperty(name);
            if (a != null && b != null) b.objectReferenceValue = a.objectReferenceValue;
        }
        output.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void Validate(PlayerHUDView view)
    {
        var so = new SerializedObject(view);
        foreach (string field in new[] { "heartFill", "levelText", "expSlider", "ammoText", "ammoRegenRoot", "ammoRegenFill", "ammoRegenRightFill" })
            if (so.FindProperty(field)?.objectReferenceValue == null) throw new InvalidOperationException("HUD reference missing: " + field);
        var left = (Image)so.FindProperty("ammoRegenFill").objectReferenceValue;
        var right = (Image)so.FindProperty("ammoRegenRightFill").objectReferenceValue;
        if (left == right || left.fillOrigin != 0 || right.fillOrigin != 1) throw new InvalidOperationException("Ammo fills must be separate, with left/right origins.");
        var regen = (GameObject)so.FindProperty("ammoRegenRoot").objectReferenceValue;
        var count = (TMP_Text)so.FindProperty("ammoText").objectReferenceValue;
        if (count.transform.IsChildOf(regen.transform)) throw new InvalidOperationException("Arrow count must remain outside the hidden regeneration group.");
        if (so.FindProperty("artifactSlots").arraySize != 3) throw new InvalidOperationException("HUD requires exactly three artifact slots.");
    }
}
#endif
