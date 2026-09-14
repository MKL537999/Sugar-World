using System.Collections.Generic;
using UnityEngine;

public class WeaponFork : WeaponBase
{
    public float attackRange = 5f;
    public float lungeSpeed = 25f;
    public GameObject forkVisual;

    [Header("Evolution — Giant Fork")]
    public bool isGiant;

    [Header("Evolution — Orbiting Forks")]
    public int orbitCount;               // 0 = not evolved; 2 on first pick, +1 each re-pick
    public float orbitRadius = 2.4f;
    public float orbitSpeedDeg = 120f;

    private readonly List<GameObject> orbitForks = new List<GameObject>();
    private float orbitAngle;
    private float orbitHitTimer;

    private enum ForkState { Idle, Lunging, Returning }
    private ForkState state = ForkState.Idle;
    private Vector2 lungeTarget;
    private Vector2 lungeDirection;
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    private const float HitRadius = 0.8f;
    private const float ReachThreshold = 0.1f;
    private const float OrbitHitInterval = 0.35f;
    private const float OrbitForkHitRadius = 0.75f;

    public override void Initialize(PlayerController owner)
    {
        base.Initialize(owner);
        if (config != null)
        {
            baseDamage = config.forkBaseDamage;
            baseCooldown = config.forkBaseCooldown;
            attackRange = config.forkAttackRange;
            lungeSpeed = config.forkLungeSpeed;
        }
        // Detach from player so fork world position isn't affected by player movement
        transform.SetParent(null);
        state = ForkState.Idle;
    }

    public override void OnUpdate()
    {
        if (player == null) return;

        switch (state)
        {
            case ForkState.Idle:
                UpdateIdle();
                break;
            case ForkState.Lunging:
                UpdateLunging();
                break;
            case ForkState.Returning:
                UpdateReturning();
                break;
        }

        UpdateOrbit();
    }

    // ---- Evolution: Giant Fork — bigger fork, more damage & range, slightly slower ----
    public void EvolveGiant()
    {
        if (isGiant) return;
        isGiant = true;
        baseDamage *= 1.8f;
        attackRange *= 1.5f;
        cooldownMultiplier *= 1.15f;
        if (forkVisual != null)
            forkVisual.transform.localScale *= 1.6f;
    }

    // ---- Evolution: Orbiting Forks — forks circle the player and hurt on contact ----
    public void EvolveOrbit()
    {
        // First pick grants 2 forks; every re-pick adds one, spins faster and orbits wider
        orbitCount = orbitCount == 0 ? 2 : orbitCount + 1;
        if (orbitCount > 2)
        {
            orbitRadius += 0.45f;
            orbitSpeedDeg += 40f;
        }
        RebuildOrbitForks();
    }

    private void RebuildOrbitForks()
    {
        foreach (var f in orbitForks)
            if (f != null) Destroy(f);
        orbitForks.Clear();

        for (int i = 0; i < orbitCount; i++)
        {
            GameObject fork;
            if (forkVisual != null)
            {
                fork = Instantiate(forkVisual);
                fork.name = "OrbitFork";
                var osr = fork.GetComponentInChildren<SpriteRenderer>();
                if (osr != null) osr.sortingOrder = 6;
            }
            else
            {
                fork = new GameObject("OrbitFork");
            }
            orbitForks.Add(fork);
        }
    }

    private void UpdateOrbit()
    {
        if (orbitCount <= 0 || player == null) return;

        orbitAngle += orbitSpeedDeg * Time.deltaTime;
        float step = 360f / orbitCount;
        for (int i = 0; i < orbitForks.Count; i++)
        {
            var f = orbitForks[i];
            if (f == null) continue;
            float a = orbitAngle + i * step;
            Vector3 offset = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0f) * orbitRadius;
            f.transform.position = player.transform.position + offset;
            // +90° offset aligns the tines radially outward (fork sprite points down by default)
            f.transform.rotation = Quaternion.Euler(0f, 0f, a + 90f);
        }

        // Contact damage on a fixed tick — no per-enemy tracking needed
        orbitHitTimer -= Time.deltaTime;
        if (orbitHitTimer <= 0f)
        {
            orbitHitTimer = OrbitHitInterval;
            float dmg = CurrentDamage * 0.5f;
            foreach (var f in orbitForks)
            {
                if (f == null) continue;
                var hits = Physics2D.OverlapCircleAll(f.transform.position, OrbitForkHitRadius);
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<Enemy>(out var enemy) && enemy.IsAlive)
                        enemy.TakeDamage(dmg);
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var f in orbitForks)
            if (f != null) Destroy(f);
    }

    private void UpdateIdle()
    {
        transform.position = player.transform.position;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            var target = FindNearestEnemy();
            if (target != null)
            {
                lungeTarget = target.position;
                lungeDirection = (lungeTarget - (Vector2)player.transform.position).normalized;
                hitEnemies.Clear();
                state = ForkState.Lunging;
                RotateForkVisual(lungeDirection);

                // Immediate hit check — handles enemies overlapping the player
                CheckHits();
            }
            else
            {
                cooldownTimer = CurrentCooldown;
            }
        }
    }

    private void UpdateLunging()
    {
        transform.position = Vector2.MoveTowards(transform.position, lungeTarget, lungeSpeed * Time.deltaTime);
        CheckHits();

        if (Vector2.Distance(transform.position, lungeTarget) < ReachThreshold)
            state = ForkState.Returning;
    }

    private void UpdateReturning()
    {
        Vector2 playerPos = player.transform.position;
        transform.position = Vector2.MoveTowards(transform.position, playerPos, lungeSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, playerPos) < ReachThreshold)
        {
            transform.position = playerPos;
            state = ForkState.Idle;
            cooldownTimer = CurrentCooldown;
        }
    }

    private void CheckHits()
    {
        // Check at fork position AND player position to catch overlapping enemies
        var hits = Physics2D.OverlapCircleAll(transform.position, HitRadius);
        if (player != null)
        {
            var playerHits = Physics2D.OverlapCircleAll(player.transform.position, HitRadius);
            if (playerHits.Length > 0)
            {
                var combined = new Collider2D[hits.Length + playerHits.Length];
                System.Array.Copy(hits, combined, hits.Length);
                System.Array.Copy(playerHits, 0, combined, hits.Length, playerHits.Length);
                hits = combined;
            }
        }

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy) && enemy.IsAlive && !hitEnemies.Contains(enemy))
            {
                hitEnemies.Add(enemy);
                enemy.TakeDamage(CurrentDamage);
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        var enemies = FindObjectsOfType<Enemy>();
        Transform nearest = null;
        float minDist = attackRange;
        Vector2 playerPos = player.transform.position;

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            float dist = Vector2.Distance(enemy.transform.position, playerPos);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy.transform;
            }
        }
        return nearest;
    }

    private void RotateForkVisual(Vector2 dir)
    {
        if (forkVisual != null)
        {
            // Fork sprite points DOWN by default; +90° offset aligns tines with lunge direction
            float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            forkVisual.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        }
    }

    protected override void Attack()
    {
        // Handled in OnUpdate via state machine
    }
}
