using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DifficultyScaling
{
    private List<DifficultyTier> tiers;
    private int currentTierIndex = 0;

    public DifficultyTier CurrentTier =>
        (tiers != null && tiers.Count > 0) ? tiers[currentTierIndex] : null;

    public void Initialize(List<DifficultyTier> difficultyTiers)
    {
        tiers = new List<DifficultyTier>(difficultyTiers);
        tiers.Sort((a, b) => a.timeToActivate.CompareTo(b.timeToActivate));
        currentTierIndex = 0;
    }

    public void UpdateDifficulty(float elapsedTime)
    {
        if (tiers == null || tiers.Count == 0) return;

        int nextIndex = currentTierIndex + 1;

        if (nextIndex < tiers.Count &&
            elapsedTime >= tiers[nextIndex].timeToActivate)
        {
            currentTierIndex = nextIndex;
            Debug.Log($"Difficulty increased to Tier: {tiers[currentTierIndex].tierName}");
        }
    }

    public int GetMaxTotalCreatures(int fallbackValue)
    {
        return CurrentTier != null
            ? CurrentTier.maxTotalCreatures
            : fallbackValue;
    }

    public void Reset()
    {
        currentTierIndex = 0;
    }
}