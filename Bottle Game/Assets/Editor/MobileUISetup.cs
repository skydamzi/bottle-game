using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// UI-only migration. Combat objects and persistent UnityEvent targets are reused.
public static class MobileUISetup
{
    public const string PrefabPath = "Assets/Prefabs/UI/ExpeditionUI.prefab";
    private static readonly Color PanelColor = new Color(0.035f, 0.07f, 0.095f, 0.96f);
    private static readonly Color ActionColor = new Color(0.07f, 0.34f, 0.28f);

    [MenuItem("Bottle Game/UI/Rebuild Mobile UI Sample")]
    public static void Install()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        BuildPrefab();
        ConfigureMainMenu();
        ConfigureGameplay();
        AssetDatabase.SaveAssets();
        UICustomizationSetup.Install();
        Debug.Log("MOBILE_UI INSTALL COMPLETE: authored prefab, reusable screens, safe areas, mobile controls, both scenes connected.");
    }

    [MenuItem("Bottle Game/UI/Rebuild Mobile UI Sample", true)]
    private static bool CanInstall() => !EditorApplication.isPlayingOrWillChangePlaymode;

    public static void ConnectGameplay()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            BuildPrefab();
        }
        ConfigureGameplay();
        UICustomizationSetup.Install();
    }

    private static void BuildPrefab()
    {
        RectTransform root = Rect("ExpeditionCanvas", null);
        root.gameObject.SetActive(false);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        ConfigureScaler(root.gameObject.AddComponent<CanvasScaler>());
        ExpeditionUIView view = root.gameObject.AddComponent<ExpeditionUIView>();

        RectTransform hudCanvas = CanvasLayer("HUDCanvas", root, 100, false);
        view.hudLayer = hudCanvas.gameObject.AddComponent<CanvasGroup>();
        view.hudLayer.alpha = 0f;
        view.hudLayer.interactable = view.hudLayer.blocksRaycasts = false;
        RectTransform hudSafe = SafeArea("SafeArea", hudCanvas);
        RectTransform hud = Panel("Combat HUD", hudSafe, new Color(0.025f, 0.05f, 0.07f, 0.8f), false);
        hud.anchorMin = new Vector2(0.32f, 1f);
        hud.anchorMax = Vector2.one;
        hud.pivot = new Vector2(0.5f, 1f);
        hud.offsetMin = new Vector2(4f, -590f);
        hud.offsetMax = new Vector2(-24f, -196f);
        Vertical(hud, 16, 8);
        view.roundStatus = Paragraph("Round Status", hud, "탐험 준비", 32, FontStyle.Bold);
        FixedHeight(view.roundStatus.rectTransform, 120f);
        view.targetingStatus = Paragraph("Targeting Status", hud, "자동 조준 대기", 30);
        FixedHeight(view.targetingStatus.rectTransform, 76f);
        view.buildSummary = Paragraph("Build Summary", hud, "아티팩트 없음", 26);
        FixedHeight(view.buildSummary.rectTransform, 144f);
        ((Text)view.buildSummary).resizeTextForBestFit = true;
        ((Text)view.buildSummary).resizeTextMinSize = 20;
        ((Text)view.buildSummary).resizeTextMaxSize = 26;
        view.feedback = Label("Artifact Feedback", hudSafe, "", 38, FontStyle.Bold);
        ((Text)view.feedback).alignment = TextAnchor.MiddleCenter;
        view.feedback.color = new Color(0.38f, 1f, 0.8f);
        view.feedback.rectTransform.anchorMin = new Vector2(0.04f, 0.6f);
        view.feedback.rectTransform.anchorMax = new Vector2(0.96f, 0.69f);
        view.feedback.rectTransform.offsetMin = view.feedback.rectTransform.offsetMax = Vector2.zero;

        RectTransform modal = CanvasLayer("ModalCanvas", root, 200, true);
        view.modalLayer = modal.gameObject.AddComponent<CanvasGroup>();
        RectTransform shield = Panel("Decision Overlay", modal, new Color(0.01f, 0.025f, 0.04f, 0.92f), true);
        Stretch(shield);
        RectTransform modalSafe = SafeArea("SafeArea", shield);
        RectTransform scrollRoot = Panel("Decision Scroll", modalSafe, PanelColor, true);
        Stretch(scrollRoot, new Vector2(24f, 32f), new Vector2(-24f, -32f));
        ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;
        RectTransform viewport = Rect("Viewport", scrollRoot);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect("Decision Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        Vertical(content, 24, 28);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        scroll.viewport = viewport;
        view.decisionScroll = scroll;

        RectTransform preparing = Screen("PreparingScreen", content);
        view.preparingScreen = preparing.gameObject;
        Paragraph("Heading", preparing, "쓰레기 대륙 탐험", 56, FontStyle.Bold);
        view.preparingDescription = Paragraph("Instructions", preparing, "탐험 중 아티팩트를 모으고 보스를 처치하세요.", 38);
        view.startButton = Action("StartButton", preparing, "탐험 시작");
        view.preparingReturnButton = Action("ReturnButton", preparing, "마을로 귀환", true);

        RectTransform rewards = Screen("RewardScreen", content);
        view.rewardScreen = rewards.gameObject;
        Paragraph("Heading", rewards, "라운드 클리어!", 56, FontStyle.Bold);
        Paragraph("Instructions", rewards, "아티팩트 하나를 선택하세요", 38);
        view.rewardCards = new ExpeditionChoiceView[3];
        for (int i = 0; i < 3; i++) view.rewardCards[i] = Choice("RewardCard_0" + (i + 1), rewards, i, true);
        view.emptyRewards = Paragraph("EmptyRewards", rewards, "획득 가능한 아티팩트가 없습니다.", 38).gameObject;
        view.skipRewardButton = Action("SkipButton", rewards, "계속");

        RectTransform routes = Screen("RouteScreen", content);
        view.routeScreen = routes.gameObject;
        Paragraph("Heading", routes, "어느 길로 갈까요?", 56, FontStyle.Bold);
        view.routeCards = new ExpeditionChoiceView[3];
        for (int i = 0; i < 3; i++) view.routeCards[i] = Choice("RouteCard_0" + (i + 1), routes, i, false);

        RectTransform visit = Screen("VisitScreen", content);
        view.visitScreen = visit.gameObject;
        view.visitTitle = Paragraph("Heading", visit, "방문", 56, FontStyle.Bold);
        view.visitDescription = Paragraph("Description", visit, "", 38);
        view.purchaseHint = Paragraph("PurchaseHint", visit, "", 34);
        view.purchaseButton = Action("PurchaseButton", visit, "회복 구매");
        view.continueButton = Action("ContinueButton", visit, "다음 전투로");

        RectTransform result = Screen("ResultScreen", content);
        view.resultScreen = result.gameObject;
        view.resultTitle = Paragraph("Heading", result, "탐험 결과", 56, FontStyle.Bold);
        view.resultSummary = Paragraph("RewardsSummary", result, "", 38);
        view.restartButton = Action("RestartButton", result, "다시 탐험");
        view.resultReturnButton = Action("ReturnButton", result, "마을로 귀환", true);
        view.buildDetails = Paragraph("BuildDetails", content, "", 34);

        rewards.gameObject.SetActive(false);
        routes.gameObject.SetActive(false);
        visit.gameObject.SetActive(false);
        result.gameObject.SetActive(false);
        view.buildDetails.gameObject.SetActive(false);
        root.gameObject.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
    }

    private static void ConfigureGameplay()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        ExpeditionManager manager = Object.FindObjectsOfType<ExpeditionManager>(true).Single(item => item.gameObject.scene == scene);
        PlayerMovement movement = manager.players[0].GetComponent<PlayerMovement>();
        PlayerAimController aim = manager.players[0].GetComponent<PlayerAimController>();
        Button swap = FindScene<Button>(scene, "WeaponSwapButton");
        Canvas controls = swap.GetComponentInParent<Canvas>();
        RectTransform ui = Root(scene);
        controls.name = "ControlsCanvas";
        controls.transform.SetParent(ui, false);
        ConfigureScaler(controls.GetComponent<CanvasScaler>());
        controls.sortingOrder = 0;
        RectTransform safe = GetSafeArea(controls.transform);
        RectTransform hud = Group("HUD", safe);
        RectTransform inputs = Group("Controls", safe);
        RectTransform utilities = Group("UtilityButtons", safe);
        foreach (Transform child in controls.transform.Cast<Transform>().ToArray())
            if (child != safe) child.SetParent(hud, false);
        ReparentRect(swap.transform, inputs);
        ReparentRect(movement.joystick.transform, inputs);
        ReparentRect(aim.joystick.transform, inputs);
        SetRect((RectTransform)movement.joystick.transform, new Vector2(0f, 0f), new Vector2(200f, 230f), new Vector2(320f, 320f));
        SetRect((RectTransform)aim.joystick.transform, new Vector2(1f, 0f), new Vector2(-200f, 230f), new Vector2(320f, 320f));
        SetRect((RectTransform)swap.transform, new Vector2(1f, 0f), new Vector2(-120f, 500f), new Vector2(176f, 176f));
        Button menu = FindScene<Button>(scene, "MainMenu");
        ReparentRect(menu.transform, utilities);
        SetRect((RectTransform)menu.transform, Vector2.one, new Vector2(-104f, -104f), new Vector2(160f, 160f));
        Transform portrait = hud.Find("Portrait_Frame");
        if (portrait != null) SetRect((RectTransform)portrait, new Vector2(0f, 1f), new Vector2(184f, -184f), new Vector2(320f, 320f));
        NormalizeRaycasts(controls.gameObject);
        StyleButton(swap);
        StyleButton(menu);
        Get<CanvasGroup>(controls.gameObject);
        MobileGameplayControls gate = Get<MobileGameplayControls>(controls.gameObject);
        gate.expedition = manager;
        gate.player = movement;
        MobileUIRoot root = Get<MobileUIRoot>(ui.gameObject);
        root.referenceCanvas = controls;

        ExpeditionUIView view = ui.GetComponentInChildren<ExpeditionUIView>(true);
        if (view == null)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            view = ((GameObject)PrefabUtility.InstantiatePrefab(asset, ui)).GetComponent<ExpeditionUIView>();
        }
        manager.GetComponent<ExpeditionUI>().view = view;
        Save(scene);
    }

    private static void ConfigureMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        Button start = FindScene<Button>(scene, "Conquest!");
        Canvas canvas = start.GetComponentInParent<Canvas>();
        RectTransform ui = Root(scene);
        canvas.name = "MainMenuCanvas";
        canvas.transform.SetParent(ui, false);
        ConfigureScaler(canvas.GetComponent<CanvasScaler>());
        RectTransform safe = GetSafeArea(canvas.transform);
        foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
            if (child != safe) child.SetParent(safe, false);
        SetRect((RectTransform)start.transform, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(720f, 160f));
        Text startText = start.GetComponentInChildren<Text>();
        if (startText != null)
        {
            Stretch(startText.rectTransform, new Vector2(20f, 12f), new Vector2(-20f, -12f));
            startText.fontSize = 48;
            startText.alignment = TextAnchor.MiddleCenter;
        }
        Transform title = safe.Find("Title");
        if (title != null)
        {
            RectTransform rect = (RectTransform)title;
            rect.anchorMin = new Vector2(0.07f, 0.68f);
            rect.anchorMax = new Vector2(0.93f, 0.88f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Text label = title.GetComponent<Text>();
            if (label != null) { label.fontSize = 80; label.alignment = TextAnchor.MiddleCenter; label.horizontalOverflow = HorizontalWrapMode.Wrap; }
        }
        Transform navigation = safe.Find("BottomNavigationBar");
        if (navigation != null)
        {
            RectTransform rect = (RectTransform)navigation;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 200f);
        }
        NormalizeRaycasts(canvas.gameObject);
        StyleButton(start);
        Get<MobileUIRoot>(ui.gameObject).referenceCanvas = canvas;
        Save(scene);
    }

    private static RectTransform Root(Scene scene)
    {
        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(obj => obj.name == "UIRoot");
        return existing != null ? (RectTransform)existing.transform : Rect("UIRoot", null);
    }
    private static RectTransform GetSafeArea(Transform parent)
    {
        Transform existing = parent.Find("SafeArea");
        return existing != null ? (RectTransform)existing : SafeArea("SafeArea", parent);
    }
    private static RectTransform Group(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return (RectTransform)existing;
        RectTransform rect = Rect(name, parent);
        Stretch(rect);
        return rect;
    }
    private static void ReparentRect(Transform child, Transform parent) => child.SetParent(parent, false);
    private static T FindScene<T>(Scene scene, string name) where T : Component => Object.FindObjectsOfType<T>(true).Single(item => item.gameObject.scene == scene && item.name == name);
    private static T Get<T>(GameObject obj) where T : Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();
    private static void Save(Scene scene)
    {
        foreach (GameObject obj in scene.GetRootGameObjects())
            foreach (Component component in obj.GetComponentsInChildren<Component>(true))
                if (component != null) { EditorUtility.SetDirty(component); if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
    }
    private static void NormalizeRaycasts(GameObject root)
    {
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        foreach (Button button in root.GetComponentsInChildren<Button>(true)) if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
        foreach (Joystick joystick in root.GetComponentsInChildren<Joystick>(true))
        {
            SerializedProperty background = new SerializedObject(joystick).FindProperty("background");
            RectTransform rect = background.objectReferenceValue as RectTransform;
            Graphic graphic = rect != null ? rect.GetComponent<Graphic>() : null;
            if (graphic != null) graphic.raycastTarget = true;
        }
    }
    private static void StyleButton(Button button)
    {
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        ColorBlock colors = button.colors;
        colors.pressedColor = new Color(0.65f, 0.8f, 0.77f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        foreach (Text label in button.GetComponentsInChildren<Text>(true)) label.raycastTarget = false;
    }
    private static RectTransform CanvasLayer(string name, Transform parent, int order, bool raycaster)
    {
        RectTransform rect = Rect(name, parent);
        Stretch(rect);
        Canvas canvas = rect.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = order;
        if (raycaster) rect.gameObject.AddComponent<GraphicRaycaster>();
        return rect;
    }
    private static RectTransform SafeArea(string name, Transform parent)
    {
        RectTransform rect = Rect(name, parent);
        Stretch(rect);
        rect.gameObject.AddComponent<MobileSafeArea>();
        return rect;
    }
    private static RectTransform Screen(string name, Transform parent)
    {
        RectTransform rect = Rect(name, parent);
        Vertical(rect, 0, 24);
        return rect;
    }
    private static ExpeditionChoiceView Choice(string name, Transform parent, int index, bool icon)
    {
        RectTransform rect = Panel(name, parent, ActionColor, true);
        LayoutElement size = rect.gameObject.AddComponent<LayoutElement>();
        size.minHeight = 240f;
        HorizontalLayoutGroup row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(24, 24, 24, 24);
        row.spacing = 20f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        StyleButton(button);
        Image image = null;
        if (icon)
        {
            image = Panel("Icon", rect, Color.white, false).GetComponent<Image>();
            image.preserveAspect = true;
            LayoutElement iconSize = image.gameObject.AddComponent<LayoutElement>();
            iconSize.preferredWidth = iconSize.preferredHeight = 88f;
            iconSize.flexibleWidth = 0f;
        }
        RectTransform body = Rect("Body", rect);
        body.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Vertical(body, 0, 12);
        ExpeditionChoiceView view = rect.gameObject.AddComponent<ExpeditionChoiceView>();
        view.button = button;
        view.title = Paragraph("Title", body, "선택지", 42, FontStyle.Bold);
        view.description = Paragraph("Description", body, "효과 설명", 36);
        view.icon = image;
        view.choiceIndex = index;
        return view;
    }
    private static Button Action(string name, Transform parent, string text, bool secondary = false)
    {
        RectTransform rect = Panel(name, parent, secondary ? new Color(0.15f, 0.2f, 0.24f) : ActionColor, true);
        rect.gameObject.AddComponent<LayoutElement>().minHeight = 144f;
        VerticalLayoutGroup layout = Vertical(rect, 24, 0);
        layout.childForceExpandHeight = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        StyleButton(button);
        Text label = Paragraph("Label", rect, text, 44, FontStyle.Bold);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
    private static Text Paragraph(string name, Transform parent, string text, int size, FontStyle style = FontStyle.Normal)
    {
        Text label = Label(name, parent, text, size, style);
        LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = size * 1.4f;
        layout.minWidth = 0f; // Wrap long localized text within the parent's width.
        return label;
    }
    private static Text Label(string name, Transform parent, string text, int size, FontStyle style)
    {
        Text label = Rect(name, parent).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = new Color(0.93f, 0.97f, 0.96f);
        label.raycastTarget = false;
        label.supportRichText = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }
    private static VerticalLayoutGroup Vertical(RectTransform rect, int padding, float spacing)
    {
        VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }
    private static void FixedHeight(RectTransform rect, float height)
    {
        LayoutElement element = rect.GetComponent<LayoutElement>();
        element.minHeight = element.preferredHeight = height;
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        if (parent != null) rect.SetParent(parent, false);
        return rect;
    }
    private static RectTransform Panel(string name, Transform parent, Color color, bool raycast)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return rect;
    }
    private static void Stretch(RectTransform rect) => Stretch(rect, Vector2.zero, Vector2.zero);
    private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minimum;
        rect.offsetMax = maximum;
    }
    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
