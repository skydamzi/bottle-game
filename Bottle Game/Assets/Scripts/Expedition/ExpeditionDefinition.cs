using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExpeditionRewardTable
{
    public string tableId;
    public ArtifactCatalog catalog;
}

[CreateAssetMenu(menuName = "Bottle Game/Expedition/Expedition")]
public class ExpeditionDefinition : ScriptableObject
{
    public List<ExpeditionRoundDefinition> rounds = new List<ExpeditionRoundDefinition>();
    public ArtifactCatalog artifactCatalog;
    public List<ExpeditionRewardTable> rewardTables = new List<ExpeditionRewardTable>();
    public string returnSceneName = "MainMenu";

    public ArtifactCatalog ResolveRewardTable(string tableId)
    {
        foreach (ExpeditionRewardTable table in rewardTables)
            if (table != null && table.tableId == tableId && table.catalog != null) return table.catalog;
        return artifactCatalog;
    }
}
