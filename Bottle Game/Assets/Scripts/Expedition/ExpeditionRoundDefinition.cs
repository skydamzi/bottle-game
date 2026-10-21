using System;
using System.Collections.Generic;
using UnityEngine;

public enum ExpeditionRoundType { Normal, Special, Boss }
public enum ExpeditionClearCondition { AllRequiredEnemiesDefeated }

[Serializable]
public class ExpeditionEnemySpawn
{
    public GameObject prefab;
    public Vector2 offset;
    [Min(1)] public int count = 1;
    public bool required = true;
    [Min(0.01f)] public float healthMultiplier = 1f;
    [Min(0f)] public float damageMultiplier = 1f;
    [Min(0f)] public float speedMultiplier = 1f;
}

[CreateAssetMenu(menuName = "Bottle Game/Expedition/Round")]
public class ExpeditionRoundDefinition : ScriptableObject
{
    public string roundId;
    public string displayName;
    public ExpeditionRoundType roundType;
    public ExpeditionClearCondition clearCondition;
    public List<ExpeditionEnemySpawn> enemies = new List<ExpeditionEnemySpawn>();
    public string rewardTableId = "default";
    [Min(0)] public int currencyReward = 10;
    public List<ExpeditionRouteDefinition> nextRoutes = new List<ExpeditionRouteDefinition>();
}
