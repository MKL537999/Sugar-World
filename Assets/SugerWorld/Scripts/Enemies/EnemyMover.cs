using System.Collections;
using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    private Enemy enemy;
    private Rigidbody2D rb;
    private Transform player;
    private SpriteRenderer sr;

    [Header("Ranged Settings")]
    public float shootRange = 7f;
    public float shootCooldown = 3.5f;
    public float projectileSpeed = 6f;

    [Header("Charger Settings")]
    public float chargeWindupTime = 0.8f;
    public float chargeSpeed = 14f;
    public float chargeDuration = 0.5f;
    public float chargeCooldown = 4f;
    public float chargeTriggerRange = 6f;

    [Header("Separation")]
    public float separationRadius = 0.9f;   // enemies keep this distance from each other
    public float separationSpeed = 2.2f;    // push strength while crowded
    private static readonly Collider2D[] separationBuffer = new Collider2D[32];

    private enum ChargeState { Approach, Windup, Charging, Recovering }
    private ChargeState chargeState = ChargeState.Approach;
    private float shootTimer;
    private float chargeTimer;
    private Vector2 chargeDir;
    private Color originalColor;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    private void Start()
    {
        if (PlayerController.Instance != null)
            player = PlayerController.Instance.transform;
        shootTimer = shootCooldown;
    }

    private void FixedUpdate()
    {
        if (!enemy.IsAlive || player == null) return;

        switch (enemy.attackType)
        {
            case EnemyAttackType.Melee:
                UpdateMelee();
                break;
            case EnemyAttackType.Ranged:
                UpdateRanged();
                break;
            case EnemyAttackType.Charger:
                UpdateCharger();
                break;
        }
    }

    // ---- Melee: chase player directly ----
    private void UpdateMelee()
    {
        Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
        rb.velocity = dir * enemy.MoveSpeed + GetSeparation() * separationSpeed;
        if (sr != null) sr.flipX = dir.x < 0f;
    }

    // ---- Separation: steer away from nearby enemies so the horde does not stack ----
    private Vector2 GetSeparation()
    {
        Vector2 myPos = transform.position;
        int hits = Physics2D.OverlapCircleNonAlloc(myPos, separationRadius, separationBuffer);
        Vector2 sep = Vector2.zero;
        for (int i = 0; i < hits; i++)
        {
            var col = separationBuffer[i];
            if (col == null || col.transform == transform) continue;
            if (col.GetComponent<Enemy>() == null) continue;   // only enemies (and the boss) repel

            Vector2 away = myPos - (Vector2)col.ClosestPoint(myPos);
            float d = away.magnitude;
            if (d < 1e-4f)
            {
                // Deep inside another collider (e.g. under the boss) — push from its center
                away = myPos - (Vector2)col.transform.position;
                d = away.magnitude;
                if (d < 1e-4f) continue;
            }
            sep += away / d * (1f - Mathf.Clamp01(d / separationRadius));
        }
        // Cap the push so a dense pack cannot fling enemies around
        if (sep.sqrMagnitude > 1f) sep.Normalize();
        return sep;
    }

    // ---- Ranged: keep distance, shoot red balls ----
    private void UpdateRanged()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
        Vector2 sep = GetSeparation() * separationSpeed;

        if (dist > shootRange)
        {
            // Move closer
            rb.velocity = dir * enemy.MoveSpeed + sep;
        }
        else if (dist < shootRange * 0.6f)
        {
            // Back away
            rb.velocity = -dir * enemy.MoveSpeed * 0.7f + sep;
        }
        else
        {
            // Holding position — still shuffle apart from neighbors
            rb.velocity = sep;
        }

        if (sr != null) sr.flipX = dir.x < 0f;

        shootTimer -= Time.fixedDeltaTime;
        if (shootTimer <= 0f && dist <= shootRange)
        {
            shootTimer = shootCooldown;
            Shoot();
        }
    }

    private void Shoot()
    {
        var projGo = new GameObject("EnemyProjectile");
        projGo.transform.position = transform.position;
        projGo.transform.localScale = Vector3.one * 0.30f;

        // Outer glow halo — additive red halo behind the core
        var glowSr = projGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = GetGlowSprite();
        glowSr.color = new Color(1f, 0.2f, 0.1f, 1f);
        glowSr.sortingOrder = 3;
        glowSr.material = GetGlowMaterial();

        // Glossy candy core — opaque saturated red with white shine, default material
        // (normal alpha blending keeps the red crisp; matches the candy art style)
        var coreGo = new GameObject("Core");
        coreGo.transform.SetParent(projGo.transform);
        coreGo.transform.localPosition = Vector3.zero;
        var coreSr = coreGo.AddComponent<SpriteRenderer>();
        coreSr.sprite = GetBallSprite();
        coreSr.color = Color.white;
        coreSr.sortingOrder = 4;

        var col = projGo.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        var proj = projGo.AddComponent<EnemyProjectile>();
        Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
        proj.Fire(dir, enemy.damage, projectileSpeed);
    }

    private static Material glowMaterial;
    private static Sprite glowSprite;
    private static Sprite ballSprite;

    public static Material GetGlowMaterial()
    {
        if (glowMaterial == null)
        {
            var shader = Shader.Find("SugarWorld/ProjectileGlow");
            if (shader != null)
                glowMaterial = new Material(shader);
        }
        return glowMaterial;
    }

    // White radial gradient — tinted red-orange, rendered additively as halo
    public static Sprite GetGlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        float coreRadius = size * 0.35f;
        float glowRadius = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha;
                if (dist <= coreRadius) alpha = 1f;
                else if (dist <= glowRadius) alpha = Mathf.InverseLerp(glowRadius, coreRadius, dist);
                else alpha = 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return glowSprite;
    }

    // Solid vivid candy-red ball with glossy white highlight and darker rim
    public static Sprite GetBallSprite()
    {
        if (ballSprite != null) return ballSprite;
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        float radius = size * 0.32f;
        Vector2 shineCenter = center + new Vector2(-radius * 0.35f, radius * 0.35f);
        float shineRadius = radius * 0.35f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x, y);
                float dist = Vector2.Distance(p, center);
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }
                // Glossy highlight (upper-left shine, like the candy sprites)
                if (Vector2.Distance(p, shineCenter) <= shineRadius)
                {
                    tex.SetPixel(x, y, new Color(1f, 0.97f, 0.95f, 1f));
                    continue;
                }
                // Vivid saturated red, slightly darker rim for volume
                float edge = Mathf.InverseLerp(radius, radius * 0.3f, dist);
                float r = Mathf.Lerp(0.8f, 1f, edge);
                float g = Mathf.Lerp(0.04f, 0.09f, edge);
                tex.SetPixel(x, y, new Color(r, g, 0.1f, 1f));
            }
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        ballSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ballSprite;
    }

    // ---- Charger: approach, red glow windup, then dash ----
    private void UpdateCharger()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        switch (chargeState)
        {
            case ChargeState.Approach:
                Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
                rb.velocity = dir * enemy.MoveSpeed + GetSeparation() * separationSpeed;
                if (sr != null) sr.flipX = dir.x < 0f;

                if (dist <= chargeTriggerRange)
                {
                    chargeState = ChargeState.Windup;
                    chargeTimer = chargeWindupTime;
                    StartCoroutine(RedGlow());
                }
                break;

            case ChargeState.Windup:
                rb.velocity = Vector2.zero;
                chargeTimer -= Time.fixedDeltaTime;
                if (chargeTimer <= 0f)
                {
                    chargeDir = ((Vector2)(player.position - transform.position)).normalized;
                    chargeState = ChargeState.Charging;
                    chargeTimer = chargeDuration;
                }
                break;

            case ChargeState.Charging:
                rb.velocity = chargeDir * chargeSpeed;
                chargeTimer -= Time.fixedDeltaTime;
                if (chargeTimer <= 0f)
                {
                    chargeState = ChargeState.Recovering;
                    chargeTimer = chargeCooldown;
                }
                break;

            case ChargeState.Recovering:
                rb.velocity = Vector2.zero;
                chargeTimer -= Time.fixedDeltaTime;
                if (chargeTimer <= 0f)
                {
                    chargeState = ChargeState.Approach;
                }
                break;
        }
    }

    private IEnumerator RedGlow()
    {
        // Pulsing red glow during windup
        float elapsed = 0f;
        while (elapsed < chargeWindupTime && chargeState == ChargeState.Windup)
        {
            float pulse = (Mathf.Sin(elapsed * 20f) + 1f) * 0.5f;
            if (sr != null)
                sr.color = Color.Lerp(originalColor, new Color(1f, 0.2f, 0.2f), 0.4f + pulse * 0.6f);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        // Flash bright red right before charging
        if (sr != null) sr.color = new Color(1f, 0.1f, 0.1f);
    }
}
