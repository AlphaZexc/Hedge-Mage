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