using UnityEngine;
using System.Collections.Generic;

public enum ExpeditionRouteType { NormalCombat, EliteCombat, Recovery, Shop, RandomEvent }

[System.Serializable]
public class ExpeditionEventOutcome
{
    public string outcomeId;
    [TextArea] public string description;
    [Min(1)] public int weight = 1;
    [Min(0)] public int currency;
    [Range(0f, 1f)] public float healFraction;
}

[CreateAssetMenu(menuName = "Bottle Game/Expedition/Route")]
public class ExpeditionRouteDefinition : ScriptableObject
{
    public string routeId;
    public string displayName;
    [TextArea] public string description;
    public ExpeditionRouteType routeType;
    [Range(0f, 1f)] public float healFraction = 0.3f;
    [Min(0)] public int cost = 10;
    [Min(1f)] public float attackMultiplier = 1.4f;
    public List<ExpeditionEventOutcome> eventOutcomes = new List<ExpeditionEventOutcome>
    {
        new ExpeditionEventOutcome { outcomeId = "scrap", description = "폐품 더미 발견: 고철 15개를 얻었습니다.", currency = 15 },
        new ExpeditionEventOutcome { outcomeId = "water", description = "깨끗한 물 발견: 최대 HP의 20%를 회복했습니다.", healFraction = 0.2f }
    };
}
