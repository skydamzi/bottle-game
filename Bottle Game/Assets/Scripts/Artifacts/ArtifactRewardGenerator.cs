using System;
using System.Collections.Generic;

// No global Unity random state. The authority supplies its random source/seed.
public sealed class ArtifactRewardGenerator
{
    private readonly Random random;

    public ArtifactRewardGenerator(Random randomSource)
    {
        random = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
    }

    public List<ArtifactDefinition> Generate(ArtifactCatalog catalog, RunBuild build, int count = 3)
    {
        var result = new List<ArtifactDefinition>();
        if (catalog == null || catalog.artifacts == null || build == null || count <= 0) return result;
        var available = new List<ArtifactDefinition>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (ArtifactDefinition artifact in catalog.artifacts)
        {
            if (artifact == null || string.IsNullOrWhiteSpace(artifact.artifactId) ||
                artifact.weight <= 0f || float.IsNaN(artifact.weight) || float.IsInfinity(artifact.weight) ||
                !build.CanAcquire(artifact) || !ids.Add(artifact.artifactId)) continue;
            available.Add(artifact);
        }
        while (result.Count < count && available.Count > 0)
        {
            double total = 0;
            foreach (ArtifactDefinition artifact in available) total += artifact.weight;
            double roll = random.NextDouble() * total;
            int selected = available.Count - 1;
            for (int i = 0; i < available.Count; i++)
            {
                roll -= available[i].weight;
                if (roll < 0) { selected = i; break; }
            }
            result.Add(available[selected]);
            available.RemoveAt(selected);
        }
        return result;
    }
}
