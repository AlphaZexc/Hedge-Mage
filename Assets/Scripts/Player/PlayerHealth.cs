using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    private AStarGridManager gridManager;
    public static PlayerHealth Instance;

    [Header("Health Settings")]
    public int maxLives = 3;
    public int maxHealth = 100;
    public float invincibilityDuration = 2f;
    public float damageCooldown = 1f;
    public Transform spawnPoint;

    [Header("UI References")]
    public Image lifeImage;
    public Sprite life3Sprite;
    public Sprite life2Sprite;
    public Sprite life1Sprite;
    public TMP_Text healthText;
    public TMP_Text timerText;

    [Header("Animation")]
    public Animator animator;

    private int currentLives;
    private int currentHealth;
    private float lastDamageTime = -10f;
    private bool isDead = false;
    public bool IsDead => isDead;

    private float levelStartTime;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        gridManager = FindFirstObjectByType<AStarGridManager>();
        playerMovement = GetComponent<PlayerMovement>();
        ResetForNewLevel();
    }

    void Update()
    {
        if (!isDead && timerText != null)
        {
            float elapsedTime = Time.time - levelStartTime;
            int minutes = Mathf.FloorToInt(elapsedTime / 60F);
            int seconds = Mathf.FloorToInt(elapsedTime % 60F);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    public void ResetForNewLevel()
    {

        // Snap player to the center of the closest tile
        Vector3Int playerCell = gridManager.walkableTilemap.WorldToCell(transform.position);
        Vector3 snappedPos = gridManager.walkableTilemap.GetCellCenterWorld(playerCell);
        transform.position = snappedPos;


        currentLives = maxLives;
        currentHealth = maxHealth;
        isDead = false;
        lastDamageTime = -10f;
        levelStartTime = Time.time;

        transform.position = spawnPoint != null ? spawnPoint.position : transform.position;

        if (animator != null)
            animator.SetBool("isDead", false);

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);

        ResetTimer();
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        if (Time.time - lastDamageTime < damageCooldown || isDead) return;

        Debug.Log($"PlayerHealth.TakeDamage called! Damage: {damage}, CurrentHealth before: {currentHealth}");
        currentHealth -= damage;
        lastDamageTime = Time.time;
        UpdateUI();

        // Trigger screen shake
        var camFollow = FindFirstObjectByType<CameraFollow>();
        if (camFollow != null)
            camFollow.Shake();

        // Trigger candle snuff/relight effect
        var candle = GetComponentInChildren<FlickeringLight2D>();
        if (candle != null)
            candle.SnuffAndRelight();

        if (currentHealth <= 0)
        {
            currentLives--;
            if (currentLives <= 0)
            {
                currentHealth = 0;
                UpdateUI();
                StartCoroutine(HandlePlayerDeath());
            }
            else
            {
                currentHealth = maxHealth;
                StartCoroutine(HandleRespawn());
            }
        }
    }

    private IEnumerator HandleRespawn()
    {
        isDead = true;
        if (animator != null) animator.SetTrigger("Die");
        if (playerMovement != null) playerMovement.SetMovementEnabled(false);

        yield return new WaitForSeconds(1f);

        transform.position = spawnPoint.position;
        isDead = false;

        if (playerMovement != null) playerMovement.SetMovementEnabled(true);

        UpdateUI();
    }

    private IEnumerator HandlePlayerDeath()
    {
        isDead = true;
        if (animator != null) animator.SetTrigger("Die");
        if (playerMovement != null) playerMovement.SetMovementEnabled(false);

        yield return new WaitForSeconds(1f);

        float finalTime = Time.time - levelStartTime;
        LevelPopupManager.Instance?.ShowLevelFailPopup(finalTime);
    }

    private void UpdateUI()
    {
        if (lifeImage != null)
        {
            lifeImage.sprite = currentLives switch
            {
                3 => life3Sprite,
                2 => life2Sprite,
                1 => life1Sprite,
                _ => null
            };
            lifeImage.enabled = currentLives > 0;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth} HP";
        }
    }

    private void ResetTimer()
    {
        if (timerText != null)
        {
            timerText.text = "00:00";
        }
    }

    public float GetElapsedLevelTime()
    {
        return Time.time - levelStartTime;
    }
}






// using UnityEngine;
// using TMPro;
// using System; // Needed for TimeSpan formatting
// using UnityEngine.UI; // For Image component

// public class PlayerHealth : MonoBehaviour
// {
//     [Header("Health Settings")]
//     public int maxLives = 3;
//     public int maxHealth = 100;
//     public float respawnDelay = 2f;
//     public float damageCooldown = 1f;
//     public float respawnImmunityTime = 1.5f;
//     public Transform spawnPoint;

//     [Header("UI References")]
//     public Image lifeImage; // NEW: UI Image to show life sprite
//     public Sprite life3Sprite;
//     public Sprite life2Sprite;
//     public Sprite life1Sprite;
//     public TMP_Text healthText;
//     public TMP_Text timerText; // Reference to the *in-game* timer UI text

//     [Header("Animation")]
//     public Animator animator;
//     private PlayerMovement playerMovement;

//     private int currentLives;
//     private int currentHealth;
//     private Vector3 initialSpawnPoint;
//     private float lastDamageTime = -10f;
//     private bool isDead = false;
//     public bool IsDead => isDead;
//     private float finalTime = -1f;


//     private bool levelEnded = false;

//     public static PlayerHealth Instance { get; private set; }

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     void Start()
//     {
//         currentLives = maxLives;
//         currentHealth = maxHealth;
//         playerMovement = GetComponent<PlayerMovement>();
//         initialSpawnPoint = spawnPoint != null ? spawnPoint.position : transform.position;
//         transform.position = initialSpawnPoint;
//         UpdateUI();
//         levelEnded = false;
//         Time.timeScale = 1f;
//     }

//     void Update()
//     {
//         if (!levelEnded && timerText != null)
//         {
//             float timeElapsed = Time.timeSinceLevelLoad;
//             int minutes = Mathf.FloorToInt(timeElapsed / 60F);
//             int seconds = Mathf.FloorToInt(timeElapsed % 60F);
//             timerText.text = $"{minutes:00}:{seconds:00}";
//         }
//     }

//     public void MoveToLevelStart(Transform startPoint)
//     {
//         transform.position = startPoint.position;
//     }


//     public void TakeDamage(int damageTaken)
//     {
//         if (!IsAlive() || isDead || Time.time - lastDamageTime < damageCooldown) return;

//         currentHealth -= damageTaken;
//         currentHealth = Mathf.Max(currentHealth, 0);
//         lastDamageTime = Time.time;
//         UpdateUI();

//         if (currentHealth <= 0)
//         {
//             StartDeathSequence();
//         }
//     }

//     void StartDeathSequence()
//     {
//         if (!IsAlive() || isDead) return;

//         isDead = true;
//         if (animator != null)
//         {
//             animator.SetTrigger("Die");
//         }

//         if (playerMovement != null)
//         {
//             playerMovement.SetMovementEnabled(false);
//         }

//         Invoke(nameof(LoseLife), 0.5f);
//     }

//     void LoseLife()
//     {
//         currentLives--;
//         UpdateUI();

//         if (currentLives > 0)
//         {
//             Invoke(nameof(Respawn), respawnDelay);
//         }
//         else
//         {
//             levelEnded = true;
//             finalTime = Time.timeSinceLevelLoad; //inserted

//             if (LevelPopupManager.Instance != null)
//             {
//                 // float finalTime = Time.timeSinceLevelLoad; removed for insterted
//                 LevelPopupManager.Instance.ShowLevelFailPopup(finalTime);
//             }
//             else
//             {
//                 Debug.LogWarning("LevelPopupManager instance not found. Cannot show fail popup.");
//             }
//         }
//     }


//     void Respawn()
//     {
//         transform.position = initialSpawnPoint;
//         currentHealth = maxHealth;
//         lastDamageTime = Time.time - (damageCooldown - respawnImmunityTime);
//         isDead = false;
//         UpdateUI();

//         if (playerMovement != null)
//         {
//             playerMovement.SetMovementEnabled(true);
//         }
//     }

//     bool IsAlive()
//     {
//         return currentLives > 0;
//     }

//     void UpdateUI()
//     {
//         if (lifeImage != null)
//         {
//             switch (currentLives)
//             {
//                 case 3:
//                     lifeImage.sprite = life3Sprite;
//                     lifeImage.enabled = true;
//                     break;
//                 case 2:
//                     lifeImage.sprite = life2Sprite;
//                     lifeImage.enabled = true;
//                     break;
//                 case 1:
//                     lifeImage.sprite = life1Sprite;
//                     lifeImage.enabled = true;
//                     break;
//                 default:
//                     lifeImage.enabled = false;
//                     break;
//             }
//         }

//         if (healthText != null)
//         {
//             healthText.text = $"{currentHealth} HP";
//         }
//     }


//     // public void ResetForNewLevel()
//     // {
//     //     currentHealth = maxHealth;
//     //     isDead = false;
//     //     lastDamageTime = Time.time;
//     //     transform.position = initialSpawnPoint;

//     //     if (playerMovement != null)
//     //     {
//     //         playerMovement.SetMovementEnabled(true);
//     //     }

//     //     UpdateUI();
//     // }
//     //INSERTED 5PM 5/1
//     public void ResetTimer()
//     {
//         // Reset the in-game timer UI to 00:00
//         if (timerText != null)
//         {
//             timerText.text = "00:00";
//         }
//     }

//     public void ResetForNewLevel()
//     {
//         currentHealth = maxHealth;
//         isDead = false;
//         lastDamageTime = Time.time;
//         transform.position = initialSpawnPoint;

//         if (playerMovement != null)
//         {
//             playerMovement.SetMovementEnabled(true);
//         }

//         // Reset the timer
//         ResetTimer();

//         UpdateUI();
//     }




//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (other.CompareTag("KillZone"))
//         {
//             if (Time.time - lastDamageTime < respawnImmunityTime || !IsAlive() || isDead) return;

//             currentHealth = 0;
//             UpdateUI();
//             StartDeathSequence();
//         }

//         LetterObject letterObj = other.GetComponent<LetterObject>();
//         if (letterObj != null)
//         {
//             if (WordProgressManager.Instance != null)
//             {
//                 WordProgressManager.Instance.CollectLetter(letterObj.letter);
//             }
//             else
//             {
//                 Debug.LogWarning("WordProgressManager instance not found. Cannot collect letter.");
//             }
//             Destroy(other.gameObject);
//         }

//         if (other.CompareTag("LevelEndTrigger"))
//         {
//             CompleteLevel();
//         }
//     }
//     // inserted code
//     public void CompleteLevel()
//     {
//         if (levelEnded) return;

//         levelEnded = true;
//         finalTime = Time.timeSinceLevelLoad;

//         if (playerMovement != null)
//         {
//             playerMovement.SetMovementEnabled(false);
//         }

//         if (LevelPopupManager.Instance != null)
//         {
//             LevelPopupManager.Instance.ShowLevelCompletePopup(finalTime);
//         }
//         else
//         {
//             Debug.LogWarning("LevelPopupManager instance not found. Cannot show complete popup.");
//         }

//         Debug.Log("Level Complete Triggered!");
//     } 
// }