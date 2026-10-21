using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class UICustomizationTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        SceneManager.LoadScene("SampleScene");
        yield return UIFrames();
    }

    [UnityTearDown]
    public IEnumerator TearDown() { yield return new ExitPlayMode(); }

    [UnityTest]
    public IEnumerator CustomThemeCardsTMPAndWholeLayoutKeepSessionCommands()
    {
        ExpeditionManager manager = Object.FindObjectOfType<ExpeditionManager>();
        ExpeditionUI original = manager.GetComponent<ExpeditionUI>();
        Assert.IsEmpty(ExpeditionUIViewInspector.GetErrors(original.view));
        UITheme defaultTheme = AssetDatabase.LoadAssetAtPath<UITheme>(UICustomizationSetup.ThemePath);
        Assert.AreSame(defaultTheme, original.view.GetComponent<UIThemeScope>().theme);
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(UICustomizationSetup.RewardPath));
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(UICustomizationSetup.RoutePath));

        // No image/size/state should change when a style is empty.
        GameObject probe = new GameObject("Theme probe", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIThemeBinding));
        Image image = probe.GetComponent<Image>();
        Button button = probe.GetComponent<Button>();
        button.targetGraphic = image;
        UIThemeBinding binding = probe.GetComponent<UIThemeBinding>();
        binding.role = UIThemeRole.PrimaryButton;
        binding.image = image;
        binding.selectable = button;
        image.color = Color.magenta;
        Vector2 originalSize = image.rectTransform.sizeDelta;
        UITheme theme = ScriptableObject.CreateInstance<UITheme>();
        binding.Apply(theme);
        Assert.AreEqual(Color.magenta, image.color);
        Assert.AreEqual(originalSize, image.rectTransform.sizeDelta);
        Texture2D texture = new Texture2D(8, 8);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 2f);
        theme.primaryButton.sprite = sprite;
        theme.primaryButton.applyTint = true;
        theme.primaryButton.tint = Color.white;
        theme.primaryButton.applyButtonStates = true;
        theme.primaryButton.transition = Selectable.Transition.SpriteSwap;
        theme.primaryButton.spriteStates = new SpriteState { pressedSprite = sprite };
        binding.Apply(theme);
        Assert.AreSame(sprite, image.sprite);
        Assert.AreEqual(Image.Type.Sliced, image.type);
        Assert.AreEqual(Color.white, image.color);
        Assert.AreEqual(Selectable.Transition.SpriteSwap, button.transition);
        Assert.AreSame(sprite, button.spriteState.pressedSprite);
        Assert.AreEqual(originalSize, image.rectTransform.sizeDelta);
        Debug.Log("UI_CUSTOMIZATION PASS theme sprite/9-slice/button states and authored layout preservation");

        // An inactive template lets us configure references before the clone's Awake.
        GameObject templates = new GameObject("Inactive UI templates");
        templates.SetActive(false);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(MobileUISetup.PrefabPath);
        ExpeditionUIView indexTemplate = Object.Instantiate(source, templates.transform).GetComponent<ExpeditionUIView>();
        foreach (ExpeditionChoiceView card in indexTemplate.rewardCards) card.choiceIndex = 0;
        foreach (ExpeditionChoiceView card in indexTemplate.routeCards) card.choiceIndex = 0;
        ExpeditionUIView indexProbe = Object.Instantiate(indexTemplate, GameObject.Find("UIRoot").transform);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indexProbe.rewardCards.Select(card => card.choiceIndex).ToArray(), "Authored duplicated cards must use slot order, even without prefab overrides");
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indexProbe.routeCards.Select(card => card.choiceIndex).ToArray());
        Object.Destroy(indexProbe.gameObject);
        ExpeditionUIView layout = Object.Instantiate(source, templates.transform).GetComponent<ExpeditionUIView>();
        ExpeditionChoiceView rewardTemplate = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UICustomizationSetup.RewardPath), templates.transform).GetComponent<ExpeditionChoiceView>();
        ExpeditionChoiceView routeTemplate = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UICustomizationSetup.RoutePath), templates.transform).GetComponent<ExpeditionChoiceView>();
        rewardTemplate.tintBackgroundByRarity = false;
        rewardTemplate.button.image.sprite = sprite;
        rewardTemplate.button.image.color = Color.cyan;
        routeTemplate.tintBackgroundByRarity = false;
        routeTemplate.button.image.sprite = sprite;
        TMP_FontAsset tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        Assert.IsNotNull(tmpFont);
        GameObject tmpObject = new GameObject("Imported TMP title", typeof(RectTransform));
        tmpObject.SetActive(false);
        TextMeshProUGUI tmp = tmpObject.AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(rewardTemplate.title.transform.parent, false);
        tmp.font = tmpFont;
        tmp.raycastTarget = false;
        rewardTemplate.title.gameObject.SetActive(false);
        rewardTemplate.title = tmp;
        tmp.gameObject.SetActive(true); // Parent is inactive, so Awake still waits for the real clone.
        layout.rewardCardPrefab = rewardTemplate;
        layout.routeCardPrefab = routeTemplate;
        original.enabled = false;
        GameObject host = new GameObject("Replacement UI controller");
        host.SetActive(false);
        ExpeditionUI controller = host.AddComponent<ExpeditionUI>();
        controller.expedition = manager;
        controller.view = original.view;
        controller.viewPrefab = layout;
        controller.viewParent = GameObject.Find("UIRoot").transform;
        host.SetActive(true);
        yield return UIFrames();
        ExpeditionUIView view = controller.view;
        Assert.AreNotSame(original.view, view);
        Assert.IsFalse(original.view.gameObject.activeInHierarchy);
        Assert.AreEqual(3, view.rewardCards.Length);
        Assert.AreEqual(3, view.routeCards.Length);
        Assert.IsTrue(view.rewardCards.All(card => card.title is TMP_Text));
        Assert.IsTrue(view.rewardCards.Select(card => card.GetInstanceID()).Distinct().Count() == 3);
        int objectCount = view.GetComponentsInChildren<Transform>(true).Length;
        int startRequests = 0;
        view.StartRequested += () => startRequests++;
        for (int i = 0; i < 4; i++) { controller.enabled = false; controller.enabled = true; }
        yield return UIFrames();
        Click(view.startButton);
        Assert.AreEqual(1, startRequests);
        Assert.AreEqual(ExpeditionState.Combat, manager.State);
        foreach (MonsterStats enemy in manager.spawner.SpawnedEnemies.ToArray()) enemy.OnDamage(100000f, null);
        Assert.AreEqual(ExpeditionState.Reward, manager.State);
        yield return UIFrames();
        int chosenIndex = manager.RewardCandidates.Count - 1;
        ExpeditionChoiceView chosen = view.rewardCards[chosenIndex];
        Assert.AreEqual(manager.RewardCandidates[chosenIndex].displayName, ((TMP_Text)chosen.title).text);
        Assert.AreEqual(Color.cyan, chosen.button.image.color, "Rarity data must not recolor imported card artwork");
        Assert.AreSame(sprite, chosen.button.image.sprite);
        Assert.IsFalse(ExpeditionChoiceView.SetText(chosen.title, ((TMP_Text)chosen.title).text), "Unchanged text must not dirty TMP geometry");
        Click(chosen.button);
        chosen.button.onClick.Invoke(); // Stale inactive card must not grant a second reward.
        Assert.AreEqual(1, manager.Session.Rewards.Count);
        Assert.AreEqual(ExpeditionState.RouteSelection, manager.State);
        yield return UIFrames();
        int recoveryIndex = Enumerable.Range(0, manager.AvailableRoutes.Count).First(i => manager.AvailableRoutes[i].routeType == ExpeditionRouteType.Recovery);
        Click(view.routeCards[recoveryIndex].button);
        Assert.AreEqual(ExpeditionState.Transition, manager.State);
        yield return UIFrames();
        Click(view.continueButton);
        Assert.AreEqual(ExpeditionState.Combat, manager.State);
        Assert.AreEqual(1, manager.RoundIndex);
        Assert.AreEqual(objectCount, view.GetComponentsInChildren<Transform>(true).Length, "Replacement cards must be created once, not on every refresh/enable");
        Assert.IsEmpty(ExpeditionUIViewInspector.GetErrors(view));
        Object.Destroy(probe);
        Object.Destroy(templates);
        Object.Destroy(sprite);
        Object.Destroy(texture);
        // UI objects are removed on Play Mode exit; the TMP font belongs to the imported asset.
        Debug.Log("UI_CUSTOMIZATION COMPLETE: theme, TMP, whole layout, three reusable cards, preserved artwork, actual raycast reward/route clicks, lifecycle and session isolation.");
    }

    private static IEnumerator UIFrames()
    {
        int until = Time.frameCount + 2;
        float timeout = Time.realtimeSinceStartup + 3f;
        while (Time.frameCount < until && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.GreaterOrEqual(Time.frameCount, until);
    }

    private static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform rect = (RectTransform)button.transform;
        ScrollRect scroll = button.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            Vector3 local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            scroll.content.anchoredPosition += Vector2.up * (scroll.viewport.rect.center.y - local.y);
            Canvas.ForceUpdateCanvases();
        }
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.IsNotEmpty(hits);
        Assert.AreSame(button, hits[0].gameObject.GetComponentInParent<Button>());
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
}
