using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Bottle Game/Artifacts/Catalog")]
public class ArtifactCatalog : ScriptableObject
{
    public List<ArtifactDefinition> artifacts = new List<ArtifactDefinition>();
}
