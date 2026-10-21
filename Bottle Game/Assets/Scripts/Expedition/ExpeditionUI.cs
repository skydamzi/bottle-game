using System.Text;
using UnityEngine;
using Unity.Profiling;

// Binds session commands/data to an authored view. No layout or object creation.
public class ExpeditionUI : MonoBehaviour
{
    public ExpeditionManager expedition;
    public Font font;
    public ExpeditionUIView view;
    [Header("Layout replacement (optional)")]
    [Tooltip("Drag a prefab with ExpeditionUIView here to replace the scene's default view at startup.")]
    public ExpeditionUIView viewPrefab;
    [Tooltip("Parent for the replacement. Assign UIRoot; leave empty to reuse the default view's parent.")]
    public Transform viewParent;
    private static readonly ProfilerMarker RefreshMarker = new ProfilerMarker("BottleGame.UI.Refresh");
    private static readonly ProfilerMarker HudMarker = new ProfilerMarker("BottleGame.UI.HUD");
    private RunBuild observedBuild;
    private PlayerStats observedPlayer;
    private PlayerMovement movement;
    private PlayerAutoTarget targeting;
    private float nextHudRefresh, feedbackUntil;
    private string inventorySummary = "아티팩트 없음";
    private string inventoryDetails = "아티팩트 없음";
    private readonly StringBuilder text = new StringBuilder(256);

    void Awake()
    {
        if (expedition == null) expedition = GetComponent<ExpeditionManager>();
        if (view == null) view = GetComponentInChildren<ExpeditionUIView>(true);
        if (viewPrefab != null)
        {
            Transform parent = viewParent != null ? viewParent : view != null ? view.transform.parent : transform;
            if (view != null) view.gameObject.SetActive(false);
            view = Instantiate(viewPrefab, parent, false);
            view.name = viewPrefab.name;
        }
        if (view != null) view.SetFont(font);
    }

    void OnEnable()
    {
        if (view == null || expedition == null) return;
        view.gameObject.SetActive(true);
        view.StartRequested += OnStart;
        view.ReturnRequested += OnReturn;
        view.RestartRequested += OnRestart;
        view.SkipRewardRequested += OnSkip;
        view.PurchaseRequested += OnPurchase;
        view.ContinueRequested += OnContinue;
        view.RewardRequested += OnReward;
        view.RouteRequested += OnRoute;
        expedition.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (expedition != null) expedition.Changed -= Refresh;
        UnbindBuild();
        if (view == null) return;
        view.StartRequested -= OnStart;
        view.ReturnRequested -= OnReturn;
        view.RestartRequested -= OnRestart;
        view.SkipRewardRequested -= OnSkip;
        view.PurchaseRequested -= OnPurchase;
        view.ContinueRequested -= OnContinue;
        view.RewardRequested -= OnReward;
        view.RouteRequested -= OnRoute;
        view.gameObject.SetActive(false);
    }

    void Update()
    {
        if (view == null || expedition == null) return;
        if (Time.unscaledTime > feedbackUntil) ExpeditionChoiceView.SetText(view.feedback, "");
        bool combat = expedition.State == ExpeditionState.Combat || expedition.State == ExpeditionState.BossCombat;
        if (!combat || Time.unscaledTime < nextHudRefresh) return;
        nextHudRefresh = Time.unscaledTime + 0.2f;
        RefreshHud();
    }

    private void BindBuild()
    {
        PlayerStats nextPlayer = null;
        if (expedition.players != null)
            foreach (PlayerStats player in expedition.players)
                if (player != null) { nextPlayer = player; break; }
        RunBuild next = nextPlayer != null ? nextPlayer.GetComponent<RunBuild>() : null;
        if (next == observedBuild && nextPlayer == observedPlayer) return;
        UnbindBuild();
        observedPlayer = nextPlayer;
        movement = nextPlayer != null ? nextPlayer.GetComponent<PlayerMovement>() : null;
        targeting = nextPlayer != null ? nextPlayer.GetComponent<PlayerAutoTarget>() : null;
        observedBuild = next;
        if (observedBuild != null)
        {
            observedBuild.OnChanged += OnBuildChanged;
            observedBuild.OnEffectTriggered += ShowFeedback;
        }
        CacheInventory();
    }

    private void UnbindBuild()
    {
        if (observedBuild != null)
        {
            observedBuild.OnChanged -= OnBuildChanged;
            observedBuild.OnEffectTriggered -= ShowFeedback;
        }
        observedBuild = null;
        observedPlayer = null;
        movement = null;
        targeting = null;
    }

    private void CacheInventory()
    {
        inventorySummary = InventoryText(false);
        inventoryDetails = InventoryText(true);
    }

    private void OnBuildChanged()
    {
        CacheInventory();
        RefreshHud();
    }

    private void ShowFeedback(string message)
    {
        ExpeditionChoiceView.SetText(view.feedback, message);
        feedbackUntil = Time.unscaledTime + 1.6f;
    }

    public void Refresh()
    {
        using var sample = RefreshMarker.Auto();
        if (view == null || expedition == null) return;
        BindBuild();
        view.Show(expedition.State);
        RefreshHud();
        switch (expedition.State)
        {
            case ExpeditionState.Preparing:
                int rounds = expedition.definition != null ? expedition.definition.rounds.Count : 0;
                ExpeditionChoiceView.SetText(view.preparingDescription, $"{rounds}개 전투를 진행하며 아티팩트를 모으세요. 이동·공격·회피는 기존 조작을 사용합니다.\n회복소·상점·이벤트 방문은 전투 횟수를 소모하지 않습니다.");
                break;
            case ExpeditionState.Reward:
                for (int i = 0; i < view.rewardCards.Length; i++)
                {
                    bool valid = i < expedition.RewardCandidates.Count;
                    if (view.rewardCards[i] == null) continue;
                    if (!valid) { view.rewardCards[i].gameObject.SetActive(false); continue; }
                    ArtifactDefinition artifact = expedition.RewardCandidates[i];
                    view.rewardCards[i].Bind(artifact.displayName, RarityName(artifact.rarity) + " · " + artifact.description, artifact.icon, view.GetRarityColor(artifact.rarity));
                }
                if (view.emptyRewards != null) view.emptyRewards.SetActive(expedition.RewardCandidates.Count == 0);
                if (view.skipRewardButton != null) view.skipRewardButton.gameObject.SetActive(expedition.RewardCandidates.Count == 0);
                break;
            case ExpeditionState.RouteSelection:
                for (int i = 0; i < view.routeCards.Length; i++)
                {
                    if (view.routeCards[i] == null) continue;
                    if (i >= expedition.AvailableRoutes.Count) { view.routeCards[i].gameObject.SetActive(false); continue; }
                    ExpeditionRouteDefinition route = expedition.AvailableRoutes[i];
                    view.routeCards[i].Bind(route.displayName, route.description, null, Color.white);
                }
                break;
            case ExpeditionState.Transition:
                ExpeditionRouteDefinition visit = expedition.CurrentVisit;
                ExpeditionChoiceView.SetText(view.visitTitle, visit != null ? visit.displayName : "다음 라운드 준비");
                ExpeditionChoiceView.SetText(view.visitDescription, expedition.VisitMessage + (visit != null ? "\n" + visit.description : ""));
                view.SetPurchase(visit != null && visit.routeType == ExpeditionRouteType.Shop, expedition.PurchaseMade, visit != null ? visit.cost : 0);
                break;
            case ExpeditionState.Completed:
            case ExpeditionState.Failed:
                ExpeditionChoiceView.SetText(view.resultTitle, expedition.State == ExpeditionState.Completed ? "탐험 성공" : "탐험 실패");
                ExpeditionChoiceView.SetText(view.resultSummary, expedition.ResultSummary + "\n\n탐험용 강화가 초기화되었습니다.");
                break;
        }
        view.RefreshLayout();
    }

    private void RefreshHud()
    {
        using var sample = HudMarker.Auto();
        if (view == null || expedition == null) return;
        text.Clear();
        int total = expedition.definition != null ? expedition.definition.rounds.Count : 0;
        int round = expedition.State == ExpeditionState.Preparing ? 0 : Mathf.Min(expedition.RoundIndex + 1, total);
        text.Append("탐험 ").Append(round).Append(" / ").Append(total).Append(" · ").Append(StateName(expedition.State));
        if (expedition.CurrentRound != null) text.Append('\n').Append(expedition.CurrentRound.displayName);
        text.Append(" · 적 ").Append(expedition.AliveEnemies);
        if (expedition.Session != null) text.Append(" · 고철 ").Append(expedition.Session.Currency);
        if (expedition.players != null)
            foreach (PlayerStats player in expedition.players)
                if (player != null) text.Append("\nHP ").Append(Mathf.CeilToInt(player.currentHealth)).Append(" / ").Append(Mathf.CeilToInt(player.EffectiveMaxHealth));
        ExpeditionChoiceView.SetText(view.roundStatus, text.ToString());
        string targetText = "";
        if (targeting != null)
        {
            if (movement != null && !movement.IsSingleStick) targetText = $"수동 조준 · 사거리 {targeting.CurrentRange:0.0}";
            else if (!targeting.IsTrackingActive) targetText = "자동 조준 대기";
            else if (targeting.HasTarget) targetText = $"조준 유지: {targeting.CurrentTarget.name.Replace("(Clone)", "")}\n거리 {targeting.TargetDistance:0.0} / 사거리 {targeting.CurrentRange:0.0}";
            else targetText = $"사거리 내 타겟 없음 · 사거리 {targeting.CurrentRange:0.0}";
        }
        ExpeditionChoiceView.SetText(view.targetingStatus, targetText);
        bool activeBuild = observedBuild != null && observedBuild.IsRunActive;
        ExpeditionChoiceView.SetText(view.buildSummary, activeBuild ? inventorySummary + "\n" + observedBuild.Summary : "아티팩트 없음");
        view.SetBuildDetails(activeBuild ? "현재 빌드\n" + inventoryDetails + "\n" + observedBuild.Summary : "");
        if (!activeBuild) ExpeditionChoiceView.SetText(view.feedback, "");
    }

    private string InventoryText(bool descriptions)
    {
        if (observedBuild == null || observedBuild.Entries.Count == 0) return "아티팩트 없음";
        var value = new StringBuilder();
        foreach (ArtifactStack stack in observedBuild.Entries)
        {
            if (stack.Definition == null) continue;
            if (value.Length > 0) value.Append(descriptions ? "\n" : " · ");
            value.Append(stack.Definition.displayName).Append(" ×").Append(stack.Count);
            if (descriptions) value.Append('\n').Append(stack.Definition.description);
        }
        return value.ToString();
    }

    private void OnStart() => expedition.StartExpedition();
    private void OnReturn() => expedition.ReturnToHub();
    private void OnRestart() => expedition.RestartExpedition();
    private void OnSkip() => expedition.SkipReward();
    private void OnContinue() => expedition.ContinueJourney();
    private void OnReward(int index) => expedition.SelectReward(index);
    private void OnRoute(int index) => expedition.SelectRoute(index);
    private void OnPurchase()
    {
        if (!expedition.BuyRecovery()) ExpeditionChoiceView.SetText(view.purchaseHint, "회복이 필요 없거나 고철이 부족합니다.");
    }

    private static string StateName(ExpeditionState state)
    {
        switch (state)
        {
            case ExpeditionState.Preparing: return "준비";
            case ExpeditionState.Combat: return "전투";
            case ExpeditionState.Reward: return "보상";
            case ExpeditionState.RouteSelection: return "갈림길";
            case ExpeditionState.Transition: return "방문";
            case ExpeditionState.BossCombat: return "보스 전투";
            case ExpeditionState.Completed: return "성공";
            default: return "실패";
        }
    }
    private static string RarityName(ArtifactRarity rarity)
    {
        switch (rarity)
        {
            case ArtifactRarity.Uncommon: return "고급";
            case ArtifactRarity.Rare: return "희귀";
            case ArtifactRarity.Legendary: return "전설";
            default: return "일반";
        }
    }
}
