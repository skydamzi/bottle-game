using System.Linq;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds customization references in place. Existing layouts, sprites, events and cameras are preserved.
public static class UICustomizationSetup
{
    public const string ThemePath = "Assets/Data/UI/DefaultUITheme.asset";
    public const string RewardPath = "Assets/Prefabs/UI/RewardCard.prefab";
    public const string RoutePath = "Assets/Prefabs/UI/RouteCard.prefab";

    // ImportPackage is asynchronous. Batch automation must wait for its callback,
    // rather than using -quit and exiting before the font/shader assets are written.
    public static void InstallBatch()
    {
        bool importing = Resources.Load<TMP_Settings>("TMP Settings") == null;
        if (importing)
        {
            AssetDatabase.importPackageCompleted += OnBatchImportCompleted;
            AssetDatabase.importPackageFailed += OnBatchImportFailed;
        }
        Install();
        if (!importing) EditorApplication.Exit(0);
    }

    private static void OnBatchImportCompleted(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnBatchImportCompleted;
        AssetDatabase.importPackageFailed -= OnBatchImportFailed;
        EditorApplication.delayCall += () => { AssetDatabase.SaveAssets(); EditorApplication.Exit(0); };
    }

    private static void OnBatchImportFailed(string packageName, string error)
    {
        Debug.LogError("TMP Essential Resources import failed: " + error);
        EditorApplication.Exit(1);
    }

    [MenuItem("Bottle Game/UI/Connect Customization Slots")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureTMPResources();
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/UI")) AssetDatabase.CreateFolder("Assets/Data", "UI");
        UITheme theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        if (theme == null) { theme = ScriptableObject.CreateInstance<UITheme>(); AssetDatabase.CreateAsset(theme, ThemePath); }
        GameObject prefab = PrefabUtility.LoadPrefabContents(MobileUISetup.PrefabPath);
        try
        {
            ConnectRoot(prefab, theme);
            ExpeditionUIView view = prefab.GetComponent<ExpeditionUIView>();
            if (view.purchaseLabel == null && view.purchaseButton != null)
            {
                view.purchaseLabel = view.purchaseButton.GetComponentInChildren<TMP_Text>(true);
                if (view.purchaseLabel == null) view.purchaseLabel = view.purchaseButton.GetComponentInChildren<Text>(true);
            }
            SaveCard(view.rewardCards[0], RewardPath);
            SaveCard(view.routeCards[0], RoutePath);
            PrefabUtility.SaveAsPrefabAsset(prefab, MobileUISetup.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        ConnectScene("Assets/Scenes/MainMenu.unity", theme);
        ConnectScene("Assets/Scenes/SampleScene.unity", theme);
        AssetDatabase.SaveAssets();
        Debug.Log("UI_CUSTOMIZATION INSTALL COMPLETE: theme slots, reusable card prefabs and layout replacement references connected without rebuilding layouts.");
    }

    [MenuItem("Bottle Game/UI/Connect Customization Slots", true)]
    private static bool CanInstall() => !EditorApplication.isPlayingOrWillChangePlaymode;

    private static void EnsureTMPResources()
    {
        if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;
        UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().FirstOrDefault(item => item.name == "com.unity.textmeshpro");
        string path = package != null ? Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage") : "";
        if (!File.Exists(path)) throw new System.IO.FileNotFoundException("Installed TextMeshPro Essential Resources were not found.", path);
        Debug.Log("UI_CUSTOMIZATION importing installed TMP Essential Resources from " + path);
        AssetDatabase.ImportPackage(path, false);
    }

    private static void ConnectScene(string path, UITheme theme)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        GameObject root = scene.GetRootGameObjects().First(obj => obj.name == "UIRoot");
        ConnectRoot(root, theme);
        ExpeditionManager manager = Object.FindObjectsOfType<ExpeditionManager>(true).FirstOrDefault(item => item.gameObject.scene == scene);
        if (manager != null)
        {
            ExpeditionUI controller = manager.GetComponent<ExpeditionUI>();
            controller.viewParent = root.transform;
            EditorUtility.SetDirty(controller);
        }
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConnectRoot(GameObject root, UITheme theme)
    {
        UIThemeScope scope = root.GetComponent<UIThemeScope>();
        bool firstInstall = scope == null;
        if (scope == null) scope = root.AddComponent<UIThemeScope>();
        if (scope.theme == null) scope.theme = theme;
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            // Existing nested scopes (including the expedition prefab) own their own bindings.
            if (graphic.GetComponentInParent<UIThemeScope>(true) != scope) continue;
            if (!(graphic is Image) && !(graphic is Text) && !(graphic is TMP_Text)) continue;
            UIThemeBinding binding = graphic.GetComponent<UIThemeBinding>();
            if (binding != null) continue;
            binding = graphic.gameObject.AddComponent<UIThemeBinding>();
            binding.image = graphic as Image;
            binding.selectable = graphic.GetComponent<Selectable>();
            binding.label = graphic is Text || graphic is TMP_Text ? graphic : null;
            binding.role = Role(graphic);
            ExpeditionChoiceView card = graphic.GetComponent<ExpeditionChoiceView>();
            if (firstInstall && card != null && binding.role == UIThemeRole.RewardCard) card.tintBackgroundByRarity = true;
        }
    }

    private static UIThemeRole Role(Graphic graphic)
    {
        if (graphic is Text label) return label.fontStyle == FontStyle.Bold ? UIThemeRole.Heading : UIThemeRole.Body;
        if (graphic is TMP_Text) return UIThemeRole.Body;
        ExpeditionChoiceView card = graphic.GetComponent<ExpeditionChoiceView>();
        if (card != null) return graphic.name.StartsWith("Reward") ? UIThemeRole.RewardCard : UIThemeRole.RouteCard;
        if (graphic.name == "Decision Overlay") return UIThemeRole.Overlay;
        if (graphic.name == "Combat HUD") return UIThemeRole.HUD;
        if (graphic.name == "WeaponSwapButton") return UIThemeRole.WeaponButton;
        if (graphic.name == "MainMenu") return UIThemeRole.MenuButton;
        if (graphic.name == "Portrait_Frame") return UIThemeRole.PortraitFrame;
        if (graphic.GetComponentInParent<Joystick>() != null) return graphic.name == "Handle" ? UIThemeRole.JoystickHandle : UIThemeRole.JoystickBase;
        if (graphic.GetComponent<Button>() != null) return graphic.name.Contains("Return") ? UIThemeRole.SecondaryButton : UIThemeRole.PrimaryButton;
        return UIThemeRole.Panel;
    }

    private static void SaveCard(ExpeditionChoiceView card, string path)
    {
        // Never overwrite a template that the designer may already have edited.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) PrefabUtility.SaveAsPrefabAsset(card.gameObject, path);
    }
}
