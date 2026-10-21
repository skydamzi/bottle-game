using System;
using System.Collections.Generic;
using UnityEngine;

public enum ArtifactRarity { Common, Uncommon, Rare, Legendary }
public enum ArtifactDuplicatePolicy { Unique, Stack }
public enum ArtifactEffectType
{
    AttackDamagePercent, AttackSpeedPercent, DodgeNextAttackPercent, BulletDamagePercent,
    MaxHealthFlat, ExtraHitChance, BulletPierceCount, LowHealthDefensePercent,
    LowHealthAttackSpeedPercent, ProjectileRangePercent, DamageTakenPercent,
    AllyAttackPercent, SameTagAttackPercent, CriticalChance
}

[Serializable]
public class ArtifactEffectDefinition
{
    public ArtifactEffectType effectType;
    public float value;
    public float secondaryValue;
    public string tag;
}

[CreateAssetMenu(menuName = "Bottle Game/Artifacts/Artifact")]
public class ArtifactDefinition : ScriptableObject
{
    public string artifactId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public ArtifactRarity rarity;
    [Min(0f)] public float weight = 1f;
    public ArtifactDuplicatePolicy duplicatePolicy = ArtifactDuplicatePolicy.Stack;
    [Min(1)] public int maxStacks = 3;
    public List<ArtifactEffectDefinition> effects = new List<ArtifactEffectDefinition>();
    public List<string> tags = new List<string>();
}
