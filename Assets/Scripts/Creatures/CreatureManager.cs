using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    public bool enabled = true;
    public string creatureName;
    public GameObject creaturePrefab;
    public int maxAlive = 5;
    public float minSpawnInterval = 3.0f;
    public float maxSpawnInterval = 5.0f;
    public Transform[] spawnPoints;

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

    [Header("Mirelight Settings")]
    public bool mirelightEnabled = true;
    public GameObject mirelightPrefab;
    public GameObject lightPostPrefab;
    public Transform[] mirelightSpawnPoints;
    public int mirelightCount = 5;

    [Space(10)]
    public float minMirelightActivationTime = 15f;
    public float maxMirelightActivationTime = 30f;
    public float flickerDuration = 3f;

    [Header("Global Difficulty Tiers")]
    public List<DifficultyTier> difficultyTiers;

    [Header("Difficulty Scaling System")]
    [SerializeField] private DifficultyScaling difficultyScaling;

    private Dictionary<string, List<ManagedCreature>> activeCreatures;
    private Dictionary<string, CreatureSpawnConfig> creatureConfigMap;

    private PlayerInventory playerInventory => PlayerInventory.Instance;
    private int totalCreaturesAlive = 0;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        activeCreatures = new Dictionary<string, List<ManagedCreature>>();
        creatureConfigMap = new Dictionary<string, CreatureSpawnConfig>();

        if (difficultyScaling == null)
            difficultyScaling = new DifficultyScaling();
    }

    private void Start()
    {
        difficultyScaling.Initialize(difficultyTiers);

        if (mirelightEnabled)
        {
            PlaceInitialMirelights();
            StartCoroutine(MirelightActivationCycle());
        }

        StartContinuousSpawning();
        StartCoroutine(ManageCreatureLifetimesRoutine());
    }

    private void Update()
    {
        difficultyScaling.UpdateDifficulty(Time.time);
    }

    #endregion

    #region Mirelights

    private Coroutine mirelightCycleRoutine;

    private void PlaceInitialMirelights()
    {
        if (mirelightSpawnPoints == null || mirelightSpawnPoints.Length == 0)
            return;

        List<Transform> shuffledPoints =
            mirelightSpawnPoints.OrderBy(x => Random.value).ToList();

        int mirelightsToSpawn = Mathf.Min(mirelightCount, shuffledPoints.Count);

        for (int i = 0; i < mirelightsToSpawn; i++)
        {
            Instantiate(mirelightPrefab,
                        shuffledPoints[i].position,
                        shuffledPoints[i].rotation);
        }

        for (int i = mirelightsToSpawn; i < shuffledPoints.Count; i++)
        {
            if (lightPostPrefab != null)
            {
                Instantiate(lightPostPrefab,
                            shuffledPoints[i].position,
                            shuffledPoints[i].rotation);
            }
        }
    }

    private IEnumerator MirelightActivationCycle()
    {
        while (true)
        {
            float waitTime = Random.Range(minMirelightActivationTime, maxMirelightActivationTime);
            yield return new WaitForSeconds(waitTime);

            if (Mirelight.AllMirelights.Count == 0)
                continue;

            // All Mirelights flicker
            foreach (var m in Mirelight.AllMirelights)
            {
                if (m != null)
                    m.StartFlicker();
            }

            // Wait flicker duration
            yield return new WaitForSeconds(flickerDuration);

            // Tell all Mirelights to resolve
            foreach (var m in Mirelight.AllMirelights)
            {
                if (m != null)
                    m.ResolvePostFlicker();
            }
        }
    }

    #endregion

    #region Spawning

    public void StartContinuousSpawning()
    {
        foreach (var config in creatureConfigs)
        {
            if (!config.enabled)
                continue;

            if (!activeCreatures.ContainsKey(config.creatureName))
            {
                activeCreatures.Add(config.creatureName, new List<ManagedCreature>());
                creatureConfigMap.Add(config.creatureName, config);
            }

            if (config.creatureName.ToLower().Contains("lumenwing"))
                StartCoroutine(SpawnFlyerWhenPlayerHasLetter(config));
            else
                StartCoroutine(SpawnCreatureRoutine(config));
        }
    }

    private IEnumerator SpawnFlyerWhenPlayerHasLetter(CreatureSpawnConfig config)
    {
        while (!playerInventory.hasItem)
            yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(SpawnCreatureRoutine(config));
    }

    private IEnumerator SpawnCreatureRoutine(CreatureSpawnConfig config)
    {
        while (true)
        {
            float waitTime = Random.Range(config.minSpawnInterval, config.maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            int globalMax = difficultyScaling.GetMaxTotalCreatures(config.maxAlive);

            if (totalCreaturesAlive >= globalMax)
                continue;

            if (activeCreatures[config.creatureName].Count >= config.maxAlive)
                continue;

            SpawnCreature(config);
        }
    }

    private void SpawnCreature(CreatureSpawnConfig config)
    {
        if (config.creaturePrefab == null || config.spawnPoints.Length == 0)
            return;

        Transform spawnPoint =
            config.spawnPoints[Random.Range(0, config.spawnPoints.Length)];

        GameObject obj = Instantiate(config.creaturePrefab,
                                     spawnPoint.position,
                                     Quaternion.identity);

        ManagedCreature creature = new ManagedCreature
        {
            gameObject = obj,
            spawnTime = Time.time,
            lifetime = Random.Range(config.minLifetime, config.maxLifetime)
        };

        activeCreatures[config.creatureName].Add(creature);
        totalCreaturesAlive++;
    }

    #endregion

    #region Lifetime Management

    private IEnumerator ManageCreatureLifetimesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            foreach (var entry in activeCreatures)
            {
                string creatureName = entry.Key;
                var list = entry.Value;

                if (!creatureConfigMap.ContainsKey(creatureName))
                    continue;

                var config = creatureConfigMap[creatureName];

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var creature = list[i];

                    if (creature.gameObject == null ||
                        (config.enableLifetimeRespawn &&
                         Time.time - creature.spawnTime > creature.lifetime))
                    {
                        if (creature.gameObject != null)
                            Destroy(creature.gameObject);

                        list.RemoveAt(i);
                        totalCreaturesAlive--;
                    }
                }
            }
        }
    }

    #endregion

    #region Reset

    public void ResetCreatures()
    {
        StopAllCoroutines();

        foreach (var list in activeCreatures.Values)
            foreach (var creature in list)
                if (creature.gameObject != null)
                    Destroy(creature.gameObject);

        if (mirelightEnabled)
        {
            for (int i = Mirelight.AllMirelights.Count - 1; i >= 0; i--)
                if (Mirelight.AllMirelights[i] != null)
                    Destroy(Mirelight.AllMirelights[i].gameObject);
        }

        activeCreatures.Clear();
        creatureConfigMap.Clear();
        totalCreaturesAlive = 0;

        difficultyScaling.Reset();
        difficultyScaling.Initialize(difficultyTiers);

        Start();
    }

    #endregion
}