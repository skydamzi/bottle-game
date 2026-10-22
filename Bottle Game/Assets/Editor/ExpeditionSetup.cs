using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run once for the checked-in sample, or deliberately rebuild it from the menu.
// Existing combat components, joystick references, buttons and sandbox enemies are retained.
public static class ExpeditionSetup
{
    public const string DataPath = "Assets/Data/Expedition";
    public const string DefinitionPath = DataPath + "/SampleExpedition.asset";
    public const string CatalogPath = DataPath + "/ArtifactCatalog.asset";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Bottle Game/Expedition/Rebuild Sprint 02-03 Sample")]
    public static void Install()
    {
        EnsureFolder(DataPath + "/Artifacts");
        EnsureFolder(DataPath + "/Rounds");
        EnsureFolder(DataPath + "/Routes");
        ArtifactCatalog catalog = BuildArtifacts();
        ExpeditionDefinition definition = BuildExpedition(catalog);
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        PlayerStats[] players = UnityEngine.Object.FindObjectsOfType<PlayerStats>(true)
            .Where(player => player.gameObject.scene == scene && player.CompareTag("Player")).ToArray();
        if (players.Length != 1) throw new InvalidOperationException("SampleScene must have exactly one tagged gameplay Player with PlayerStats.");

        GameObject root = scene.GetRootGameObjects().FirstOrDefault(obj => obj.name == "Expedition Session");
        if (root == null) root = new GameObject("Expedition Session");
        EnemyWaveSpawner spawner = GetOrAdd<EnemyWaveSpawner>(root);
        ExpeditionManager manager = GetOrAdd<ExpeditionManager>(root);
        ExpeditionUI view = GetOrAdd<ExpeditionUI>(root);
        manager.definition = definition;
        manager.players = players;
        manager.spawner = spawner;
        manager.sceneEnemies = UnityEngine.Object.FindObjectsOfType<MonsterStats>(true)
            .Where(enemy => enemy.gameObject.scene == scene).Select(enemy => enemy.gameObject).ToArray();
        foreach (GameObject enemy in manager.sceneEnemies)
        {
            enemy.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
        }
        foreach (PlayerStats player in players)
        {
            GetOrAdd<RunBuild>(player.gameObject);
            GetOrAdd<PlayerTargetingView>(player.gameObject);
        }
        view.expedition = manager;
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        MobileUISetup.ConnectGameplay();
        Debug.Log("SPRINT02_03 INSTALL COMPLETE: 6 rounds, 5 routes, 12 artifacts, scene references connected; preserved sandbox Soju disabled.");
    }

    private static ExpeditionDefinition BuildExpedition(ArtifactCatalog catalog)
    {
        GameObject soju = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Soju.prefab");
        if (soju == null || soju.GetComponent<MonsterStats>() == null) throw new InvalidOperationException("Existing Soju prefab with MonsterStats was not found.");

        ExpeditionRouteDefinition normal = Route("normal", "일반 전투", "다음 전투로 진입합니다.", ExpeditionRouteType.NormalCombat);
        ExpeditionRouteDefinition elite = Route("elite", "정예 전투", "다음 전투의 적 HP·공격력 +40%. 아티팩트 보상은 동일합니다.", ExpeditionRouteType.EliteCombat);
        ExpeditionRouteDefinition recovery = Route("recovery", "회복소", "생존 플레이어의 최대 HP 30% 회복 후 다음 전투로 진행합니다.", ExpeditionRouteType.Recovery);
        ExpeditionRouteDefinition shop = Route("shop", "고철 상점", "고철 10개로 최대 HP 40% 회복을 한 번 구매할 수 있습니다.", ExpeditionRouteType.Shop);
        shop.healFraction = 0.4f;
        ExpeditionRouteDefinition random = Route("event", "수상한 쓰레기 더미", "고철 15개 획득 또는 최대 HP 20% 회복. 시드로 결과를 결정합니다.", ExpeditionRouteType.RandomEvent);
        List<ExpeditionRouteDefinition>[] routes =
        {
            new List<ExpeditionRouteDefinition> { normal, elite, recovery },
            new List<ExpeditionRouteDefinition> { normal, shop, random },
            new List<ExpeditionRouteDefinition> { normal, elite, recovery },
            new List<ExpeditionRouteDefinition> { normal, shop, random },
            new List<ExpeditionRouteDefinition> { normal, recovery, random },
            new List<ExpeditionRouteDefinition>()
        };
        string[] names = { "풀밭의 불청객", "버려진 병들의 습격", "고철 길목", "쓰레기 전초기지", "특수: 병 무리 소탕", "보스: 강화 소주병 (테스트)" };
        List<ExpeditionRoundDefinition> rounds = new List<ExpeditionRoundDefinition>();
        for (int i = 0; i < 6; i++)
        {
            ExpeditionRoundDefinition round = Asset<ExpeditionRoundDefinition>(DataPath + "/Rounds/Round" + (i + 1).ToString("00") + ".asset");
            round.roundId = "round_" + (i + 1).ToString("00");
            round.displayName = names[i];
            round.roundType = i == 5 ? ExpeditionRoundType.Boss : i == 4 ? ExpeditionRoundType.Special : ExpeditionRoundType.Normal;
            round.clearCondition = ExpeditionClearCondition.AllRequiredEnemiesDefeated;
            round.rewardTableId = "default";
            round.currencyReward = i == 5 ? 30 : i == 4 ? 20 : 10;
            round.nextRoutes = routes[i];
            round.enemies = new List<ExpeditionEnemySpawn>();
            if (i == 5)
            {
                round.enemies.Add(Enemy(soju, new Vector2(0f, 3.8f), 1, 5f, 1.3f, 0.85f));
            }
            else
            {
                round.enemies.Add(Enemy(soju, new Vector2(-1.5f, 3.4f), i == 0 ? 1 : 2, 0.65f + i * 0.1f, 0.7f + i * 0.05f, 1f));
                if (i >= 3) round.enemies.Add(Enemy(soju, new Vector2(1.6f, 2.7f), i == 4 ? 2 : 1, 0.85f, 0.9f, 1.05f));
            }
            EditorUtility.SetDirty(round);
            rounds.Add(round);
        }
        ExpeditionDefinition definition = Asset<ExpeditionDefinition>(DefinitionPath);
        definition.rounds = rounds;
        definition.artifactCatalog = catalog;
        definition.rewardTables = new List<ExpeditionRewardTable> { new ExpeditionRewardTable { tableId = "default", catalog = catalog } };
        definition.returnSceneName = "MainMenu";
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static ExpeditionEnemySpawn Enemy(GameObject prefab, Vector2 offset, int count, float health, float damage, float speed)
    {
        return new ExpeditionEnemySpawn { prefab = prefab, offset = offset, count = count, required = true, healthMultiplier = health, damageMultiplier = damage, speedMultiplier = speed };
    }

    private static ExpeditionRouteDefinition Route(string id, string name, string description, ExpeditionRouteType type)
    {
        ExpeditionRouteDefinition route = Asset<ExpeditionRouteDefinition>(DataPath + "/Routes/" + id + ".asset");
        route.routeId = id;
        route.displayName = name;
        route.description = description;
        route.routeType = type;
        route.healFraction = 0.3f;
        route.cost = 10;
        route.attackMultiplier = 1.4f;
        EditorUtility.SetDirty(route);
        return route;
    }

    private static ArtifactCatalog BuildArtifacts()
    {
        Sprite glove = SpriteAt("Assets/Resources/Sprites/BoxingGlove.png");
        Sprite neck = SpriteAt("Assets/Resources/Sprites/Bottle/BottleNeck.png");
        Sprite swap = SpriteAt("Assets/Resources/Sprites/Swap.png");
        ArtifactCatalog catalog = Asset<ArtifactCatalog>(CatalogPath);
        catalog.artifacts = new List<ArtifactDefinition>
        {
            Artifact("rusty_gear", "녹슨 톱니", "기본 공격력 +15%. 최대 3중첩.", ArtifactRarity.Common, 10f, 3, glove, new[] { "metal", "mechanical" }, Effect(ArtifactEffectType.AttackDamagePercent, 0.15f)),
            Artifact("compression_spring", "압축 스프링", "공격 속도 +15%. 펀치의 발사·회수 비율을 유지합니다. 최대 3중첩.", ArtifactRarity.Common, 10f, 3, swap, new[] { "metal", "mechanical" }, Effect(ArtifactEffectType.AttackSpeedPercent, 0.15f)),
            Artifact("broken_cap", "깨진 병뚜껑", "회피 후 다음 공격 피해 +60%. 한 공격에만 적용하며 재회피로 중복 충전되지 않습니다. 최대 2중첩.", ArtifactRarity.Uncommon, 7f, 2, glove, new[] { "metal", "pressure" }, Effect(ArtifactEffectType.DodgeNextAttackPercent, 0.6f)),
            Artifact("carbon_overcharger", "탄산 과충전기", "현재 효과: 탄환 피해 +12%. 액체·탄산 상호작용은 이후 확장 예정입니다. 최대 3중첩.", ArtifactRarity.Uncommon, 7f, 3, neck, new[] { "pressure" }, Effect(ArtifactEffectType.BulletDamagePercent, 0.12f)),
            Artifact("thick_glass", "두꺼운 유리", "최대 HP +25. 현재 HP를 회복하지는 않습니다. 최대 3중첩.", ArtifactRarity.Common, 10f, 3, neck, new[] { "glass" }, Effect(ArtifactEffectType.MaxHealthFlat, 25f)),
            Artifact("spent_battery", "폐건전지", "탄환 적중 시 20% 확률로 해당 탄환 피해의 35% 추가 타격. 추가 타격은 연쇄 발동하지 않습니다. 최대 3중첩.", ArtifactRarity.Rare, 5f, 3, swap, new[] { "metal", "electric" }, Effect(ArtifactEffectType.ExtraHitChance, 0.2f, 0.35f)),
            Artifact("scrap_piercer", "고철 관통탄", "탄환이 적 1명을 추가로 관통합니다. 같은 적을 반복 타격하지 않습니다. 최대 2중첩.", ArtifactRarity.Rare, 5f, 2, neck, new[] { "metal", "projectile" }, Effect(ArtifactEffectType.BulletPierceCount, 1f)),
            Artifact("emergency_valve", "비상 밸브", "HP 35% 이하일 때 받는 피해 -30%, 공격 속도 +25%. 최대 2중첩.", ArtifactRarity.Uncommon, 7f, 2, swap, new[] { "metal", "pressure" }, Effect(ArtifactEffectType.LowHealthDefensePercent, 0.3f, 0.35f), Effect(ArtifactEffectType.LowHealthAttackSpeedPercent, 0.25f, 0.35f)),
            Artifact("precision_nozzle", "정밀 노즐", "탄환 수명·자동 조준 사거리 +25%. 탄환 속도는 유지합니다. 최대 3중첩.", ArtifactRarity.Common, 8f, 3, neck, new[] { "metal", "projectile" }, Effect(ArtifactEffectType.ProjectileRangePercent, 0.25f)),
            Artifact("unstable_tank", "불안정한 압력통", "기본 공격력 +30%, 받는 피해 +20%. 최대 3중첩.", ArtifactRarity.Uncommon, 6f, 3, glove, new[] { "metal", "pressure" }, Effect(ArtifactEffectType.AttackDamagePercent, 0.3f), Effect(ArtifactEffectType.DamageTakenPercent, 0.2f)),
            Artifact("friendship_straw", "우정의 빨대", "3유닛 내 탐험 중인 생존 동료가 있으면 공격력 +20%. 싱글에서는 비활성. 중복 획득 불가.", ArtifactRarity.Rare, 3f, 1, swap, new[] { "cooperation" }, Effect(ArtifactEffectType.AllyAttackPercent, 0.2f, 3f)),
            Artifact("recycling_amplifier", "재활용 증폭기", "보유한 금속 태그의 아티팩트 중첩 수당 공격력 +4%. 최대 2중첩.", ArtifactRarity.Rare, 4f, 2, swap, new[] { "recycle" }, Effect(ArtifactEffectType.SameTagAttackPercent, 0.04f, 0f, "metal"))
        };
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static ArtifactEffectDefinition Effect(ArtifactEffectType type, float value, float secondary = 0f, string tag = "")
    {
        return new ArtifactEffectDefinition { effectType = type, value = value, secondaryValue = secondary, tag = tag };
    }

    private static ArtifactDefinition Artifact(string id, string name, string description, ArtifactRarity rarity, float weight, int stacks, Sprite icon, string[] tags, params ArtifactEffectDefinition[] effects)
    {
        ArtifactDefinition artifact = Asset<ArtifactDefinition>(DataPath + "/Artifacts/" + id + ".asset");
        artifact.artifactId = id;
        artifact.displayName = name;
        artifact.description = description;
        artifact.rarity = rarity;
        artifact.weight = weight;
        artifact.maxStacks = stacks;
        artifact.duplicatePolicy = stacks == 1 ? ArtifactDuplicatePolicy.Unique : ArtifactDuplicatePolicy.Stack;
        artifact.icon = icon;
        artifact.tags = new List<string>(tags);
        artifact.effects = new List<ArtifactEffectDefinition>(effects);
        EditorUtility.SetDirty(artifact);
        return artifact;
    }

    private static Sprite SpriteAt(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static T Asset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/');
        string parent = path.Substring(0, split);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
    }
}
