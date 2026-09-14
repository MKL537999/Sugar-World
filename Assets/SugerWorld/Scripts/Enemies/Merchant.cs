using UnityEngine;

// Ginger-bear merchant NPC. Wanders randomly around the map faster than
// regular enemies and never attacks. Defeating it opens the evolution shop.
public class Merchant : Enemy
{
    public Sprite[] idleSprites;      // 3-frame front-facing row of the sheet
    public float wanderRadius = 10f;
    public float restInterval = 2.5f; // pause between wander hops

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 wanderTarget;
    private float restTimer;
    private float animTimer;
    private int animFrame;

    private const float AnimFrameRate = 4f;
    private const float ArenaHalf = 26f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        PickWanderTarget();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        animTimer += Time.deltaTime;
        if (animTimer >= 1f / AnimFrameRate)
        {
            animTimer = 0f;
            animFrame++;
        }
        if (sr != null && idleSprites != null && idleSprites.Length > 0)
            sr.sprite = idleSprites[animFrame % idleSprites.Length];
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        Vector2 pos = transform.position;
        if (Vector2.Distance(pos, wanderTarget) < 0.5f)
        {
            // Arrived — rest a moment, then wander somewhere else
            rb.velocity = Vector2.zero;
            restTimer -= Time.fixedDeltaTime;
            if (restTimer <= 0f) PickWanderTarget();
            return;
        }

        Vector2 dir = wanderTarget - pos;
        rb.velocity = dir.normalized * moveSpeed;
        if (sr != null) sr.flipX = dir.x < 0f;
    }

    private void PickWanderTarget()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 candidate = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
            if (Mathf.Abs(candidate.x) < ArenaHalf && Mathf.Abs(candidate.y) < ArenaHalf)
            {
                wanderTarget = candidate;
                restTimer = restInterval;
                return;
            }
        }
        wanderTarget = Random.insideUnitCircle * (ArenaHalf * 0.8f);
        restTimer = restInterval;
    }

    // The shop is the reward — no XP/coin drops, no kill-count inflation
    protected override void Die()
    {
        EvolutionShopUI.Instance?.Open();
        Destroy(gameObject);
    }
}
