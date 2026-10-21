using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Serialized presentation references. This view never changes expedition rules.
[DisallowMultipleComponent]
public class ExpeditionUIView : MonoBehaviour
{
    [Header("Layers")]
    public CanvasGroup hudLayer;
    public CanvasGroup modalLayer;
    public ScrollRect decisionScroll;
    [Header("HUD")]
    public Graphic roundStatus;
    public Graphic targetingStatus;
    public Graphic buildSummary;
    public Graphic feedback;
    [Header("Screens")]
    public GameObject preparingScreen;
    public GameObject rewardScreen;
    public GameObject routeScreen;
    public GameObject visitScreen;
    public GameObject resultScreen;
    public Graphic preparingDescription;
    public Graphic visitTitle;
    public Graphic visitDescription;
    public Graphic purchaseHint;
    public Graphic resultTitle;
    public Graphic resultSummary;
    public Graphic buildDetails;
    public GameObject emptyRewards;
    public ExpeditionChoiceView[] rewardCards = new ExpeditionChoiceView[0];
    public ExpeditionChoiceView[] routeCards = new ExpeditionChoiceView[0];
    [Header("Optional card prefab overrides (created once)")]
    public ExpeditionChoiceView rewardCardPrefab;
    public ExpeditionChoiceView routeCardPrefab;
    [Header("Actions")]
    public Button startButton;
    public Button preparingReturnButton;
    public Button skipRewardButton;
    public Button purchaseButton;
    public Graphic purchaseLabel;
    public Button continueButton;
    public Button restartButton;
    public Button resultReturnButton;
    public event Action StartRequested, ReturnRequested, SkipRewardRequested, PurchaseRequested, ContinueRequested, RestartRequested;
    public event Action<int> RewardRequested, RouteRequested;
    private ExpeditionState shownState = (ExpeditionState)(-1);
    private UIThemeScope themeScope;

    void Awake()
    {
        ReplaceCards(rewardCards, rewardCardPrefab);
        ReplaceCards(routeCards, routeCardPrefab);
        SetChoiceIndices(rewardCards);
        SetChoiceIndices(routeCards);
        MobileUIRoot root = GetComponentInParent<MobileUIRoot>();
        if (root != null) root.ApplyFallbackFont(transform);
        themeScope = GetComponent<UIThemeScope>();
        if (themeScope != null && themeScope.enabled) themeScope.Apply();
        if (purchaseLabel == null && purchaseButton != null)
        {
            purchaseLabel = purchaseButton.GetComponentInChildren<TMP_Text>(true);
            if (purchaseLabel == null) purchaseLabel = purchaseButton.GetComponentInChildren<Text>(true);
        }
    }

    private static void ReplaceCards(ExpeditionChoiceView[] slots, ExpeditionChoiceView prefab)
    {
        if (prefab == null || slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            ExpeditionChoiceView original = slots[i];
            if (original == null) continue;
            ExpeditionChoiceView replacement = Instantiate(prefab, original.transform.parent, false);
            replacement.name = original.name;
            replacement.choiceIndex = i;
            replacement.transform.SetSiblingIndex(original.transform.GetSiblingIndex());
            RectTransform sourceRect = (RectTransform)original.transform;
            RectTransform rect = (RectTransform)replacement.transform;
            rect.anchorMin = sourceRect.anchorMin;
            rect.anchorMax = sourceRect.anchorMax;
            rect.pivot = sourceRect.pivot;
            rect.sizeDelta = sourceRect.sizeDelta;
            rect.anchoredPosition = sourceRect.anchoredPosition;
            rect.localScale = Vector3.one;
            original.gameObject.SetActive(false);
            replacement.gameObject.SetActive(true);
            slots[i] = replacement;
        }
    }

    private static void SetChoiceIndices(ExpeditionChoiceView[] slots)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++) if (slots[i] != null) slots[i].choiceIndex = i;
    }

    public Color GetRarityColor(ArtifactRarity rarity)
    {
        if (themeScope != null && themeScope.enabled && themeScope.theme != null) return themeScope.theme.GetRarityColor(rarity);
        return Color.white;
    }

    void OnEnable()
    {
        BindButton(startButton, OnStart, true);
        BindButton(preparingReturnButton, OnReturn, true);
        BindButton(skipRewardButton, OnSkip, true);
        BindButton(purchaseButton, OnPurchase, true);
        BindButton(continueButton, OnContinue, true);
        BindButton(restartButton, OnRestart, true);
        BindButton(resultReturnButton, OnReturn, true);
        if (rewardCards != null) foreach (ExpeditionChoiceView card in rewardCards) if (card != null) card.Clicked += OnReward;
        if (routeCards != null) foreach (ExpeditionChoiceView card in routeCards) if (card != null) card.Clicked += OnRoute;
    }

    void OnDisable()
    {
        BindButton(startButton, OnStart, false);
        BindButton(preparingReturnButton, OnReturn, false);
        BindButton(skipRewardButton, OnSkip, false);
        BindButton(purchaseButton, OnPurchase, false);
        BindButton(continueButton, OnContinue, false);
        BindButton(restartButton, OnRestart, false);
        BindButton(resultReturnButton, OnReturn, false);
        if (rewardCards != null) foreach (ExpeditionChoiceView card in rewardCards) if (card != null) card.Clicked -= OnReward;
        if (routeCards != null) foreach (ExpeditionChoiceView card in routeCards) if (card != null) card.Clicked -= OnRoute;
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action, bool bind)
    {
        if (button == null) return;
        if (bind) button.onClick.AddListener(action);
        else button.onClick.RemoveListener(action);
    }

    public void SetFont(Font font)
    {
        if (font == null) return;
        foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = font;
    }

    public void Show(ExpeditionState state)
    {
        bool combat = state == ExpeditionState.Combat || state == ExpeditionState.BossCombat;
        SetVisible(hudLayer, combat);
        SetVisible(modalLayer, !combat);
        SetActive(preparingScreen, state == ExpeditionState.Preparing);
        SetActive(rewardScreen, state == ExpeditionState.Reward);
        SetActive(routeScreen, state == ExpeditionState.RouteSelection);
        SetActive(visitScreen, state == ExpeditionState.Transition);
        SetActive(resultScreen, state == ExpeditionState.Completed || state == ExpeditionState.Failed);
        if (shownState != state && decisionScroll != null && decisionScroll.content != null)
        {
            decisionScroll.StopMovement();
            decisionScroll.content.anchoredPosition = Vector2.zero;
            decisionScroll.verticalNormalizedPosition = 1f;
        }
        shownState = state;
    }

    private static void SetVisible(CanvasGroup layer, bool visible)
    {
        if (layer == null) return;
        // Keep nested canvases registered across combat/reward transitions.
        // Transparent groups do not render and cannot capture touches.
        layer.alpha = visible ? 1f : 0f;
        layer.interactable = layer.blocksRaycasts = visible;
    }

    public void RefreshLayout()
    {
        // Layout groups resolve normally at the end of the frame; avoid forcing
        // a full rebuild on every health/target update.
        if (decisionScroll != null && decisionScroll.content != null) LayoutRebuilder.MarkLayoutForRebuild(decisionScroll.content);
    }

    public void SetBuildDetails(string value)
    {
        if (buildDetails == null) return;
        ExpeditionChoiceView.SetText(buildDetails, value);
        bool show = !string.IsNullOrEmpty(value) && (shownState == ExpeditionState.Reward || shownState == ExpeditionState.RouteSelection || shownState == ExpeditionState.Transition);
        SetActive(buildDetails.gameObject, show);
    }

    public void SetPurchase(bool visible, bool purchased, int cost)
    {
        if (purchaseButton != null) purchaseButton.gameObject.SetActive(visible);
        if (purchaseHint != null) purchaseHint.gameObject.SetActive(visible);
        if (purchaseButton != null) purchaseButton.interactable = !purchased;
        ExpeditionChoiceView.SetText(purchaseLabel, purchased ? "회복 구매 완료" : $"회복 구매 · {cost} 고철");
        ExpeditionChoiceView.SetText(purchaseHint, purchased ? "구매 완료" : "HP가 부족하고 고철이 충분할 때 구매할 수 있습니다.");
    }

    private static void SetActive(GameObject target, bool active) { if (target != null && target.activeSelf != active) target.SetActive(active); }

    private void OnStart() { if (shownState == ExpeditionState.Preparing) StartRequested?.Invoke(); }
    private void OnReturn() { ReturnRequested?.Invoke(); }
    private void OnSkip() { if (shownState == ExpeditionState.Reward) SkipRewardRequested?.Invoke(); }
    private void OnPurchase() { if (shownState == ExpeditionState.Transition) PurchaseRequested?.Invoke(); }
    private void OnContinue() { if (shownState == ExpeditionState.Transition) ContinueRequested?.Invoke(); }
    private void OnRestart() { if (shownState == ExpeditionState.Completed || shownState == ExpeditionState.Failed) RestartRequested?.Invoke(); }
    private void OnReward(int index) { if (shownState == ExpeditionState.Reward) RewardRequested?.Invoke(index); }
    private void OnRoute(int index) { if (shownState == ExpeditionState.RouteSelection) RouteRequested?.Invoke(index); }
}
