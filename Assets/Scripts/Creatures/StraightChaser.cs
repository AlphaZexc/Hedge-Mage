using UnityEngine;
using System.Collections.Generic;

public class StraightChaser : BaseCreature
{
    private const float STUCK_CHECK_INTERVAL = 0.2f;
    private const float STUCK_VELOCITY_THRESHOLD = 0.05f;

    private enum CreatureState { Wandering, Chasing, Cooldown, Stunned }

    [Header("Movement")]
    public float wanderSpeed = 3f;
    public float chargeSpeed = 13f;
    public float wanderSmoothing = 0.45f;
    public float straightMoveDotThreshold = 0.9f;

    [Header("Tier Escalation")]
    public int tilesForTier2Boost = 3;
    public int tilesForTier3Boost = 5;
    public float speedTier1 = 6f;
    public float damageTier1 = 30f;
    public float speedTier2 = 8f;
    public float damageTier2 = 45f;
    public float speedTier3 = 15f;
    public float damageTier3 = 60f;

    [Header("Pathing")]
    public float waypointProximity = 0.2f;
    public float pathRequestCooldown = 0.5f;

    [Header("Stun")]
    public float stunDuration = 1.0f;
    public float stunMinImpactSpeed = 9f; // must be near charge speed

    [Header("Cooldown")]
    public float cooldownAfterHit = 1f;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color wanderPathColor = Color.cyan;
    public Color chasePathColor = Color.red;
    public float waypointGizmoRadius = 0.1f;

    private CreatureState state;
    private AStarGridManager grid;
    private Animator animator;
    private PlayerHealth playerHealth;

    private List<Vector3> path;
    private int waypointIndex;
    private Vector3 lastKnownPlayerPos;
    private float lastPathRequestTime;
    private float cooldownEndTime;
    private float stunEndTime;

    private Vector2 lastMoveDir;
    private int straightMoveCount;

    private float stuckTimer;
    private Vector3 lastStuckPos;

    protected override void Start()
    {
        base.Start();

        animator = GetComponent<Animator>();
        grid = FindFirstObjectByType<AStarGridManager>();

        if (grid == null)
        {
            Debug.LogError("AStarGridManager not found.");
            enabled = false;
            return;
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        state = CreatureState.Wandering;
        StartWandering();
    }

    protected override void Update()
    {
        if (state == CreatureState.Stunned)
        {
            if (Time.time >= stunEndTime)
            {
                EnterWanderState();
            }
            return;
        }

        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            EnterWanderState();
            return;
        }

        if (state == CreatureState.Cooldown && Time.time >= cooldownEndTime)
        {
            EnterWanderState();
        }

        UpdateState();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (state == CreatureState.Stunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        HandleStuckDetection();
        FollowPath();
    }


    private void UpdateState()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);

        switch (state)
        {
            case CreatureState.Wandering:
                if (distance <= detectionRange)
                {
                    state = CreatureState.Chasing;
                    straightMoveCount = 0;
                    lastKnownPlayerPos = player.transform.position;
                    RequestPath(lastKnownPlayerPos);
                }
                break;

            case CreatureState.Chasing:
                lastKnownPlayerPos = player.transform.position;

                if (Time.time - lastPathRequestTime >= pathRequestCooldown)
                {
                    RequestPath(lastKnownPlayerPos);
                }
                break;
        }
    }

    private void FollowPath()
    {
        if (path == null || waypointIndex >= path.Count)
        {
            rb.linearVelocity = Vector2.zero;
            if (state != CreatureState.Chasing)
            {
                StartWandering();
            }
            return;
        }

        Vector2 toWaypoint = path[waypointIndex] - transform.position;
        float distance = toWaypoint.magnitude;
        Vector2 desiredDir = toWaypoint.normalized;

        Vector2 moveDir;
        float speed;

        if (state == CreatureState.Chasing)
        {
            moveDir = desiredDir;
            ApplyTierStats(out speed, out damage);
        }
        else
        {
            moveDir = Vector2.Lerp(lastMoveDir, desiredDir, wanderSmoothing).normalized;
            speed = wanderSpeed;
        }

        rb.linearVelocity = moveDir * speed;

        if (Vector2.Dot(desiredDir, lastMoveDir) < straightMoveDotThreshold)
        {
            straightMoveCount = 1;
        }

        lastMoveDir = moveDir;

        if (distance <= waypointProximity)
        {
            waypointIndex++;
            if (state == CreatureState.Chasing)
            {
                straightMoveCount++;
            }
        }
    }

    private void ApplyTierStats(out float speed, out int dmg)
    {
        if (straightMoveCount >= tilesForTier3Boost)
        {
            speed = speedTier3;
            dmg = Mathf.RoundToInt(damageTier3);
        }
        else if (straightMoveCount >= tilesForTier2Boost)
        {
            speed = speedTier2;
            dmg = Mathf.RoundToInt(damageTier2);
        }
        else
        {
            speed = speedTier1;
            dmg = Mathf.RoundToInt(damageTier1);
        }

        damage = dmg;
    }

    private void HandleStuckDetection()
    {
        if (rb.linearVelocity.magnitude > STUCK_VELOCITY_THRESHOLD)
        {
            stuckTimer = 0f;
            return;
        }

        stuckTimer += Time.fixedDeltaTime;
        if (stuckTimer < STUCK_CHECK_INTERVAL) return;

        stuckTimer = 0f;
        RequestPath(lastKnownPlayerPos);
        rb.AddForce(Random.insideUnitCircle.normalized * 800f, ForceMode2D.Impulse);
    }

    private void RequestPath(Vector3 target)
    {
        lastPathRequestTime = Time.time;

        List<Vector3> newPath = grid.FindPath(transform.position, target);

        if (newPath == null || newPath.Count == 0)
        {
            path = null;
            return;
        }

        // ONLY simplify while chasing
        if (state == CreatureState.Chasing)
        {
            newPath = SimplifyPath(newPath);
        }

        path = newPath;
        waypointIndex = 0;
    }


    private List<Vector3> SimplifyPath(List<Vector3> original)
    {
        if (original == null || original.Count < 2)
            return original;

        List<Vector3> simplified = new List<Vector3>();
        simplified.Add(original[0]);

        Vector2 lastDir = (original[1] - original[0]).normalized;

        for (int i = 1; i < original.Count - 1; i++)
        {
            Vector2 newDir = (original[i + 1] - original[i]).normalized;

            // Direction changed - keep this waypoint
            if (Vector2.Dot(lastDir, newDir) < 0.999f)
            {
                simplified.Add(original[i]);
                lastDir = newDir;
            }
        }

        simplified.Add(original[original.Count - 1]);
        return simplified;
    }


    private void StartWandering()
    {
        Node node = grid.GetRandomWalkableNode();
        if (node != null)
        {
            RequestPath(node.worldPosition);
        }
    }

    private void EnterWanderState()
    {
        state = CreatureState.Wandering;
        straightMoveCount = 0;
        StartWandering();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector2 v = rb.linearVelocity;
        animator.SetFloat("Horizontal", v.x);
        animator.SetFloat("Vertical", v.y);
        animator.SetFloat("Speed", v.sqrMagnitude);
        // animator.SetBool("Stunned", state == CreatureState.Stunned);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Wall Stun (only on initial impact)
        if (state == CreatureState.Chasing &&
            collision.gameObject.CompareTag("Wall") &&
            rb.linearVelocity.magnitude >= stunMinImpactSpeed)
        {
            EnterStunState();
            return;
        }

        // Damage (only once per contact)
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerHealth == null)
            {
                playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            }

            if (playerHealth != null && !playerHealth.IsDead)
            {
                playerHealth.TakeDamage(damage);
                state = CreatureState.Cooldown;
                cooldownEndTime = Time.time + cooldownAfterHit;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || path == null) return;

        Gizmos.color = state == CreatureState.Chasing ? chasePathColor : wanderPathColor;

        for (int i = 0; i < path.Count; i++)
        {
            Gizmos.DrawSphere(path[i], waypointGizmoRadius);
            if (i + 1 < path.Count)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }

    private void EnterStunState()
    {
        Debug.Log("Stunned!");
        state = CreatureState.Stunned;
        stunEndTime = Time.time + stunDuration;

        rb.linearVelocity = Vector2.zero;
        path = null;
        straightMoveCount = 0;
    }

}
