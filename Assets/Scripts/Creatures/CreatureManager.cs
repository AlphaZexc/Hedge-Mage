using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// (The helper classes at the top are unchanged)
#region Helper Classes
public class ManagedCreature
{
    public GameObject gameObject;
    public float spawnTime;
    public float lifetime;
}

[System.Serializable]
public class CreatureSpawnConfig
{
    [Header("Debug/Testing")]
    [Tooltip("Enable this creature for spawning and testing")]
    public bool enabled = true;
    public string creatureName;
    public GameObject creaturePrefab;
    public int maxAlive = 5;
    public float minSpawnInterval = 3.0f;
    public float maxSpawnInterval = 5.0f;
    public Transform[] spawnPoints;
    
    [Space(10)]
    [Header("Lifetime Settings")]
    public bool enableLifetimeRespawn = false;
    public float minLifetime = 80.0f;
    public float maxLifetime = 100.0f;
}

[System.Serializable]
public class DifficultyTier
{
    public string tierName;
    public float timeToActivate = 0.0f;
    public int maxTotalCreatures = 5;
}
#endregion


public class CreatureManager : MonoBehaviour
{
    public static CreatureManager Instance;

    [Header("Continuous Spawning Configurations")]
    public List<CreatureSpawnConfig> creatureConfigs;

    private PlayerInventory playerInventory;
    private bool flyerSpawnStarted = false;

    [Header("Mirelight Director Settings")]
    [Tooltip("Enable Mirelight spawning and events for testing")]
    public bool mirelightEnabled = true;
    public GameObject mirelightPrefab;
    public Transform[] mirelightSpawnPoints;
    public int mirelightCount = 5;
    [Space(10)]
    public float minMirelightActivationTime = 15f;
    public float maxMirelightActivationTime = 30f;
    public float flickerDuration = 3f;

    [Header("Global Difficulty Tiers")]
    public List<DifficultyTier> difficultyTiers;

    private Dictionary<string, List<ManagedCreature>> activeCreatures;
    private Dictionary<string, CreatureSpawnConfig> creatureConfigMap;
    
    private int currentDifficultyTierIndex = 0;
    private int totalCreaturesAlive = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        activeCreatures = new Dictionary<string, List<ManagedCreature>>();
        creatureConfigMap = new Dictionary<string, CreatureSpawnConfig>();
        // No player lookup here; will be notified when player is ready
    }

    private void Start()
    {
        difficultyTiers.Sort((a, b) => a.timeToActivate.CompareTo(b.timeToActivate));
        
        if (mirelightEnabled)
        {
            PlaceInitialMirelights();
            StartCoroutine(MirelightActivationCycle());
        }
        StartContinuousSpawning();
        
        StartCoroutine(MirelightActivationCycle());
        StartCoroutine(ManageCreatureLifetimesRoutine());
    }
    
    private void Update()
    {
        if (difficultyTiers.Count == 0) return;

        int nextTierIndex = currentDifficultyTierIndex + 1;
        if (nextTierIndex < difficultyTiers.Count)
        {
            if (Time.time >= difficultyTiers[nextTierIndex].timeToActivate)
            {
                currentDifficultyTierIndex = nextTierIndex;
                Debug.Log($"Difficulty increased to Tier: {difficultyTiers[currentDifficultyTierIndex].tierName}");
            }
        }
    }
    
    private void PlaceInitialMirelights()
    {
        if (mirelightPrefab == null || mirelightSpawnPoints.Length == 0) return;
        
        List<Transform> availableSpawnPoints = new List<Transform>(mirelightSpawnPoints);

        for (int i = 0; i < mirelightCount && availableSpawnPoints.Count > 0; i++)
        {
            int randIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[randIndex];
            Instantiate(mirelightPrefab, spawnPoint.position, spawnPoint.rotation);
            availableSpawnPoints.RemoveAt(randIndex);
        }
    }

    private IEnumerator MirelightActivationCycle()
    {
        while (true)
        {
            float waitTime = Random.Range(minMirelightActivationTime, maxMirelightActivationTime);
            yield return new WaitForSeconds(waitTime);

            var idleMirelights = Mirelight.AllMirelights.Where(m => m.IsIdle).ToList();
            if (idleMirelights.Count == 0) continue;

            foreach (var mirelight in idleMirelights)
            {
                mirelight.StartFlickering();
            }

            yield return new WaitForSeconds(flickerDuration);
            
            if (idleMirelights.Count > 0)
            {
                Mirelight chosenMirelight = idleMirelights[Random.Range(0, idleMirelights.Count)];
                chosenMirelight.Arm();

                foreach (var mirelight in idleMirelights)
                {
                    if (mirelight != chosenMirelight)
                    {
                        mirelight.StopFlickering();
                    }
                }
            }
        }
    }

    public void StartContinuousSpawning()
    {
        foreach (var config in creatureConfigs)
        {
            if (!config.enabled) continue;
            if (!activeCreatures.ContainsKey(config.creatureName))
            {
                activeCreatures.Add(config.creatureName, new List<ManagedCreature>());
                creatureConfigMap.Add(config.creatureName, config);
            }
            // Only gate Flyer spawns on player having a letter
            if (config.creatureName.ToLower().Contains("flyer"))
            {
                // Only start Flyer spawn if player is already known
                if (playerInventory != null && !flyerSpawnStarted)
                {
                    flyerSpawnStarted = true;
                    StartCoroutine(SpawnFlyerWhenPlayerHasLetter(config));
                }
            }
            else
            {
                StartCoroutine(SpawnCreatureRoutine(config));
            }
        }

    }

    private IEnumerator SpawnFlyerWhenPlayerHasLetter(CreatureSpawnConfig config)
    {
        // Robustly wait for player and PlayerInventory, and for player to have a letter
        float waitTime = 0f;
        while (playerInventory == null)
        {
            if (waitTime % 2f < 0.01f) Debug.Log("[FlyerSpawnDebug] Waiting for playerInventory via NotifyPlayerReady...");
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }
        while (!playerInventory.hasItem)
        {
            if (waitTime % 2f < 0.01f) Debug.Log("[FlyerSpawnDebug] Player has no letter, waiting...");
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }
        Debug.Log("[FlyerSpawnDebug] Player has a letter, starting Flyer spawn routine.");
        yield return StartCoroutine(SpawnCreatureRoutine(config));
    }

    // Call this from Player's Start or Awake
    public void NotifyPlayerReady(PlayerInventory inv)
    {
        playerInventory = inv;
        if (!flyerSpawnStarted && creatureConfigs != null)
        {
            foreach (var config in creatureConfigs)
            {
                if (config.creatureName.ToLower().Contains("flyer"))
                {
                    flyerSpawnStarted = true;
                    StartCoroutine(SpawnFlyerWhenPlayerHasLetter(config));
                }
            }
        }
    }
    
    public void ResetCreatures()
    {
        StopAllCoroutines();

        // This first loop is for StraightChasers and other creatures from your configs. It's safe.
        foreach (var creatureList in activeCreatures.Values)
        {
            foreach (var managedCreature in creatureList)
            {
                if (managedCreature.gameObject != null)
                {
                    Destroy(managedCreature.gameObject);
                }
            }
        }
        
        // --- MODIFIED: Replaced the faulty Mirelight loop with a safe, backwards for-loop ---
        if (mirelightEnabled)
        {
            for (int i = Mirelight.AllMirelights.Count - 1; i >= 0; i--)
            {
                if (Mirelight.AllMirelights[i] != null)
                {
                    Destroy(Mirelight.AllMirelights[i].gameObject);
                }
            }
        }

        // Now we can safely clear all the tracking data and restart.
        activeCreatures.Clear();
        creatureConfigMap.Clear();
        totalCreaturesAlive = 0;
        currentDifficultyTierIndex = 0;
        Start(); 
    }

    private IEnumerator SpawnCreatureRoutine(CreatureSpawnConfig config)
    {
        while (true)
        {
            float waitTime = Random.Range(config.minSpawnInterval, config.maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            int globalMax = (difficultyTiers.Count > 0) ? difficultyTiers[currentDifficultyTierIndex].maxTotalCreatures : config.maxAlive;

            if (activeCreatures.ContainsKey(config.creatureName) && 
                activeCreatures[config.creatureName].Count < config.maxAlive &&
                totalCreaturesAlive < globalMax)
            {
                SpawnCreature(config);
            }
        }
    }

    private void SpawnCreature(CreatureSpawnConfig config)
    {
        if (config.creaturePrefab == null || config.spawnPoints.Length == 0) return;

        Transform spawnPoint = config.spawnPoints[Random.Range(0, config.spawnPoints.Length)];
        if (spawnPoint == null) return;

        GameObject newCreatureObject = Instantiate(config.creaturePrefab, spawnPoint.position, Quaternion.identity);
        
        ManagedCreature newManagedCreature = new ManagedCreature
        {
            gameObject = newCreatureObject,
            spawnTime = Time.time,
            lifetime = Random.Range(config.minLifetime, config.maxLifetime)
        };

        activeCreatures[config.creatureName].Add(newManagedCreature);
        totalCreaturesAlive++;
    }
    
    private IEnumerator ManageCreatureLifetimesRoutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(2.0f); 

            List<ManagedCreature> creaturesToRemove = new List<ManagedCreature>();

            foreach(var creatureListEntry in activeCreatures)
            {
                string creatureName = creatureListEntry.Key;
                List<ManagedCreature> managedCreatures = creatureListEntry.Value;
                if (!creatureConfigMap.ContainsKey(creatureName)) continue;
                CreatureSpawnConfig config = creatureConfigMap[creatureName];
                creaturesToRemove.Clear();

                foreach(var creature in managedCreatures)
                {
                    if (creature.gameObject == null)
                    {
                        creaturesToRemove.Add(creature);
                        continue;
                    }
                    if (config.enableLifetimeRespawn && Time.time - creature.spawnTime > creature.lifetime)
                    {
                        Destroy(creature.gameObject);
                        creaturesToRemove.Add(creature);
                    }
                }

                if (creaturesToRemove.Count > 0)
                {
                    totalCreaturesAlive -= creaturesToRemove.Count;
                    managedCreatures.RemoveAll(c => creaturesToRemove.Contains(c));
                }
            }
        }
    }
}
