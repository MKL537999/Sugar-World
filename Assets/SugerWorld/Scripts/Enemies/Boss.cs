using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Vending-machine robot boss. Extends Enemy so all player weapons can hit it.
// Runs a cycle of attack patterns: radial lasers, bullet barrage,
// targeted slam, and rotating sweep lasers — picked at random each cycle.
// Later boss generations are configured stronger via ConfigureForGeneration.
public class Boss : Enemy
{
    [Header("Boss Visuals")]
    public Sprite[] idleSprites;     // front-view row of the sheet (hover)
    public Sprite[] attackSprites;   // enraged row of the sheet (dome glowing)
    public GameObject explosionEffectPrefab;

    [Header("Boss Movement")]
    public float hoverDistance = 6f;
    // moveSpeed is inherited from Enemy — set explicitly by GameBootstrap

    [Header("Boss Attacks")]
    public float initialAttackDelay = 3f;
    public float attackCooldown = 3.2f;
    public float laserDamage = 12f;
    public float laserTickInterval = 0.3f;
    public float projectileDamage = 9f;
    public float projectileSpeed = 6f;
    public float slamDamage = 22f;
    public float slamRadius = 3f;

    [Header("Per-Generation Scaling (adjusted by ConfigureForGeneration)")]
    public float telegraphTime = 0.9f;        // radial laser windup
    public float slamTelegraphTime = 1.1f;    // slam ground warning
    public float sweepSpeedDeg = 48f;         // sweep rotation speed (base is slightly slower than before)
    public int barrageWaves = 3;
    public int barrageBulletsPerWave = 16;

    private enum BossState { Spawning, Hover, Attacking }
    private BossState state = BossState.Spawning;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform player;

    private float spawnTimer;
    private float attackTimer;
    private int attackIndex;
    private string attackName = "";

    private float animTimer;
    private int animFrame;

    // attack FX objects are tracked so they are cleaned up if the boss dies mid-attack
    private readonly List<GameObject> attackFx = new List<GameObject>();

    private const int RadialBeamCount = 8;
    private const float BeamLength = 22f;
    private const float TelegraphWidth = 0.45f;   // thin red warning line
    private const float BeamActiveTime = 0.65f;
    private const float BeamGlowWidth = 0.85f;    // wide saturated halo of a firing beam
    private const float BeamCoreWidth = 0.25f;    // white-hot center line inside the halo
    private const int SweepBeamCount = 4;
    private const float SweepDuration = 2.4f;
    private const float SlamRiseTime = 0.22f;
    private const float SlamFallTime = 0.14f;
    private const float SlamRecoveryTime = 0.6f;
    private const float AnimFrameRate = 5f;

    private static Sprite beamSprite;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (PlayerController.Instance != null)
            player = PlayerController.Instance.transform;

        attackTimer = initialAttackDelay;
        state = BossState.Spawning;
        BossHealthBarUI.Instance?.Bind(this);
        SpawnSlamEffect(transform.position);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        switch (state)
        {
            case BossState.Spawning:
                spawnTimer += Time.deltaTime;
                if (sr != null)
                    sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(spawnTimer / 0.7f));
                if (spawnTimer >= 0.7f)
                {
                    state = BossState.Hover;
                    if (sr != null) sr.color = Color.white;
                }
                break;

            case BossState.Hover:
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    state = BossState.Attacking;
                    StartCoroutine(RunAttackCycle());
                }
                break;
        }

        UpdateAnimation();
    }

    private IEnumerator RunAttackCycle()
    {
        // Pattern picked at random each cycle — no fixed order, repeats allowed
        switch (Random.Range(0, 4))
        {
            case 0: yield return LaserRadialAttack(); break;
            case 1: yield return BarrageAttack(); break;
            case 2: yield return SlamAttack(); break;
            case 3: yield return LaserSweepAttack(); break;
        }
        attackIndex++;
        attackName = "";
        attackTimer = attackCooldown;
        state = BossState.Hover;
    }

    // Called by BossSpawner with the 0-based generation index of this boss.
    // Later bosses hit harder, telegraph less, attack more often and sweep faster.
    public void ConfigureForGeneration(int generation)
    {
        float g = generation;

        float dmgMult = Mathf.Min(4f, 1f + 0.25f * g);
        laserDamage *= dmgMult;
        projectileDamage *= Mathf.Min(4f, 1f + 0.2f * g);
        slamDamage *= Mathf.Min(4f, 1f + 0.2f * g);
        projectileSpeed *= Mathf.Min(1.5f, 1f + 0.08f * g);
        slamRadius += 0.15f * g;

        // Shorter openings to react
        telegraphTime = Mathf.Max(0.45f, telegraphTime - 0.12f * g);
        slamTelegraphTime = Mathf.Max(0.5f, slamTelegraphTime - 0.14f * g);
        attackCooldown = Mathf.Max(1.2f, attackCooldown - 0.35f * g);

        // More barrage waves for later generations; bullet count per wave stays fixed
        barrageWaves += 2 * Mathf.FloorToInt(g);
        sweepSpeedDeg += 6f * g;
    }

    private void UpdateAnimation()
    {
        var frames = state == BossState.Attacking ? attackSprites : idleSprites;
        if (frames == null || frames.Length == 0) return;
        animTimer += Time.deltaTime;
        if (animTimer >= 1f / AnimFrameRate)
        {
            animTimer = 0f;
            animFrame++;
        }
        if (sr != null) sr.sprite = frames[animFrame % frames.Length];
    }

    private void FixedUpdate()
    {
        if (state != BossState.Hover || player == null)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        // Slowly hover toward the player, keeping a comfortable firing distance
        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
        if (dist > hoverDistance + 1f)
            rb.velocity = dir * moveSpeed;
        else if (dist < hoverDistance - 1f)
            rb.velocity = -dir * moveSpeed * 0.8f;
        else
            rb.velocity = Vector2.zero;
    }

    // ---- Attack 1: 8 radial laser beams with telegraph ----
    private IEnumerator LaserRadialAttack()
    {
        attackName = "LaserRadialTelegraph";
        float baseAngle = Random.Range(0f, 45f);
        var telegraphs = new GameObject[RadialBeamCount];

        for (int i = 0; i < RadialBeamCount; i++)
        {
            float a = baseAngle + i * (360f / RadialBeamCount);
            telegraphs[i] = CreateTelegraph(transform.position, a);
        }

        // Pulsing red warning lines make the firing angles impossible to miss
        float t = 0f;
        while (t < telegraphTime)
        {
            t += Time.deltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 16f);
            foreach (var tele in telegraphs)
            {
                var tsr = tele != null ? tele.GetComponent<SpriteRenderer>() : null;
                if (tsr != null)
                    tsr.color = new Color(1f, 0.2f, 0.15f, 0.45f + 0.35f * pulse);
            }
            yield return null;
        }

        // Swap the warning lines for live beams at the exact same angles
        attackName = "LaserRadialFire";
        foreach (var tele in telegraphs)
        {
            if (tele == null) continue;
            Destroy(tele);
            attackFx.Remove(tele);
        }

        var beams = new GameObject[RadialBeamCount];
        for (int i = 0; i < RadialBeamCount; i++)
            beams[i] = CreateLaserBeam(transform.position, baseAngle + i * (360f / RadialBeamCount));

        yield return FireBeamsForTime(beams, BeamActiveTime);

        foreach (var beam in beams)
        {
            if (beam != null) Destroy(beam);
            attackFx.Remove(beam);
        }
    }

    // ---- Attack 2: radial candy bullets in waves (density scales with generation) ----
    private IEnumerator BarrageAttack()
    {
        attackName = "Barrage";
        float offset = Random.Range(0f, 360f);

        for (int w = 0; w < barrageWaves; w++)
        {
            for (int i = 0; i < barrageBulletsPerWave; i++)
            {
                float a = offset + w * (180f / barrageBulletsPerWave) + i * (360f / barrageBulletsPerWave);
                Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                SpawnBossProjectile(dir);
            }
            yield return new WaitForSeconds(0.45f);
        }
    }

    // ---- Attack 3: targeted slam — telegraph at player position, jump up, crash down ----
    private IEnumerator SlamAttack()
    {
        attackName = "SlamTelegraph";
        if (player == null) yield break;
        Vector3 target = player.position;

        var tele = new GameObject("SlamTelegraph");
        tele.transform.position = target;
        var tsr = tele.AddComponent<SpriteRenderer>();
        tsr.sprite = EnemyMover.GetGlowSprite();
        tsr.color = new Color(1f, 0.2f, 0.1f, 0.4f);
        tsr.sortingOrder = 6;
        tsr.material = EnemyMover.GetGlowMaterial();
        tele.transform.localScale = Vector3.one * (slamRadius * 2f);
        attackFx.Add(tele);

        float t = 0f;
        while (t < slamTelegraphTime)
        {
            t += Time.deltaTime;
            float pulse = 0.7f + 0.3f * Mathf.Sin(t * 18f);
            if (tsr != null) tsr.color = new Color(1f, 0.2f, 0.1f, 0.3f + 0.15f * pulse);
            yield return null;
        }

        // Rise above the target, then crash down onto it
        attackName = "SlamAir";
        Vector3 startPos = transform.position;
        Vector3 apex = target + new Vector3(0f, 9f, 0f);
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / SlamRiseTime;
            transform.position = Vector3.Lerp(startPos, apex, Mathf.Clamp01(t));
            yield return null;
        }

        attackName = "SlamImpact";
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / SlamFallTime;
            transform.position = Vector3.Lerp(apex, target, Mathf.Clamp01(t));
            yield return null;
        }

        SpawnSlamEffect(target);
        if (player != null && Vector2.Distance(player.position, target) < slamRadius + 0.4f)
            player.GetComponent<PlayerController>()?.TakeDamage(slamDamage);

        if (tele != null)
        {
            Destroy(tele);
            attackFx.Remove(tele);
        }

        yield return new WaitForSeconds(SlamRecoveryTime);
    }

    // ---- Attack 4: 4 rotating sweep lasers ----
    private IEnumerator LaserSweepAttack()
    {
        attackName = "LaserSweep";
        var beams = new GameObject[SweepBeamCount];
        float angle = Random.Range(0f, 90f);
        for (int i = 0; i < SweepBeamCount; i++)
            beams[i] = CreateLaserBeam(transform.position, angle + i * (360f / SweepBeamCount));

        float tick = 0f, elapsed = 0f;
        while (elapsed < SweepDuration)
        {
            elapsed += Time.deltaTime;
            angle += sweepSpeedDeg * Time.deltaTime;
            for (int i = 0; i < SweepBeamCount; i++)
            {
                if (beams[i] != null)
                    beams[i].transform.rotation = Quaternion.Euler(0f, 0f, angle + i * (360f / SweepBeamCount));
            }
            ApplyBeamFlicker(beams, elapsed);
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = laserTickInterval;
                DamagePlayerOnBeams(beams);
            }
            yield return null;
        }

        foreach (var beam in beams)
        {
            if (beam != null) Destroy(beam);
            attackFx.Remove(beam);
        }
    }

    // ---- helpers ----

    // Keeps damaging the player while beams are active; the width flicker makes them read as live energy
    private IEnumerator FireBeamsForTime(GameObject[] beams, float duration)
    {
        float tick = 0f, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ApplyBeamFlicker(beams, elapsed);
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = laserTickInterval;
                DamagePlayerOnBeams(beams);
            }
            yield return null;
        }
    }

    // Beam roots carry the glow width; the white core is a child and pulses along with it
    private void ApplyBeamFlicker(GameObject[] beams, float elapsed)
    {
        float pulse = 0.88f + 0.18f * (Mathf.Sin(elapsed * 34f) * 0.5f + 0.5f);
        for (int i = 0; i < beams.Length; i++)
        {
            var beam = beams[i];
            if (beam == null) continue;
            var s = beam.transform.localScale;
            beam.transform.localScale = new Vector3(s.x, BeamGlowWidth * pulse, 1f);
        }
    }

    private void DamagePlayerOnBeams(GameObject[] beams)
    {
        if (player == null) return;
        Vector2 pp = player.position;

        foreach (var beam in beams)
        {
            if (beam == null) continue;
            float a = beam.transform.eulerAngles.z;
            Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            Vector2 f = pp - (Vector2)beam.transform.position;
            float proj = Vector2.Dot(f, d);
            if (proj < 0f || proj > BeamLength) continue;
            float perp = (f - d * proj).magnitude;
            if (perp < BeamGlowWidth * 0.5f + 0.4f)
            {
                player.GetComponent<PlayerController>()?.TakeDamage(laserDamage);
                break;
            }
        }
    }

    // ---- beam factories (the beam sprite pivots at its left edge, so the GO position is the beam origin) ----

    // Thin red warning line shown before a beam fires
    private GameObject CreateTelegraph(Vector3 origin, float angleDeg)
    {
        var go = new GameObject("BossTelegraph");
        go.transform.position = origin;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        var bsr = go.AddComponent<SpriteRenderer>();
        bsr.sprite = GetBeamSprite();
        bsr.color = new Color(1f, 0.2f, 0.15f, 0.6f);
        bsr.sortingOrder = 11;
        go.transform.localScale = new Vector3(BeamLength, TelegraphWidth, 1f);
        attackFx.Add(go);
        return go;
    }

    // Firing beam: wide saturated glow with a white-hot core, both rendered additively
    private GameObject CreateLaserBeam(Vector3 origin, float angleDeg)
    {
        var go = new GameObject("BossBeam");
        go.transform.position = origin;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        var glowSr = go.AddComponent<SpriteRenderer>();
        glowSr.sprite = GetBeamSprite();
        glowSr.color = new Color(1f, 0.42f, 0.12f, 1f);   // hot orange-red halo
        glowSr.sortingOrder = 12;
        glowSr.material = EnemyMover.GetGlowMaterial();
        go.transform.localScale = new Vector3(BeamLength, BeamGlowWidth, 1f);

        var coreGo = new GameObject("Core");
        coreGo.transform.SetParent(go.transform);
        coreGo.transform.localPosition = Vector3.zero;
        var coreSr = coreGo.AddComponent<SpriteRenderer>();
        coreSr.sprite = GetBeamSprite();
        coreSr.color = Color.white;
        coreSr.sortingOrder = 13;
        coreSr.material = EnemyMover.GetGlowMaterial();
        coreGo.transform.localScale = new Vector3(1f, BeamCoreWidth / BeamGlowWidth, 1f);

        attackFx.Add(go);
        return go;
    }

    private static Sprite GetBeamSprite()
    {
        if (beamSprite != null) return beamSprite;
        // 64x64 at PPU 64 = a 1x1-unit quad, so width constants are real world units.
        // A vertical brightness falloff gives beams soft additive edges instead of hard rectangles.
        int w = 64, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float edge = Mathf.Abs(y - (h - 1) * 0.5f) / ((h - 1) * 0.5f);
            float a = Mathf.Pow(1f - edge, 1.5f);
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        beamSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 64f);
        return beamSprite;
    }

    // Candy projectile identical to the regular ranged enemies' shots
    private void SpawnBossProjectile(Vector2 dir)
    {
        var projGo = new GameObject("BossProjectile");
        projGo.transform.position = transform.position;
        projGo.transform.localScale = Vector3.one * 0.30f;

        var glowSr = projGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = EnemyMover.GetGlowSprite();
        glowSr.color = new Color(1f, 0.2f, 0.1f, 1f);
        glowSr.sortingOrder = 3;
        glowSr.material = EnemyMover.GetGlowMaterial();

        var coreGo = new GameObject("Core");
        coreGo.transform.SetParent(projGo.transform);
        coreGo.transform.localPosition = Vector3.zero;
        var coreSr = coreGo.AddComponent<SpriteRenderer>();
        coreSr.sprite = EnemyMover.GetBallSprite();
        coreSr.color = Color.white;
        coreSr.sortingOrder = 4;

        var col = projGo.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        var proj = projGo.AddComponent<EnemyProjectile>();
        proj.Fire(dir, projectileDamage, projectileSpeed);
    }

    private void SpawnSlamEffect(Vector3 pos)
    {
        if (explosionEffectPrefab == null) return;
        var fx = Instantiate(explosionEffectPrefab, pos, Quaternion.identity);
        fx.transform.localScale = Vector3.one * Mathf.Max(1f, slamRadius * 0.75f);
        foreach (var r in fx.GetComponentsInChildren<ParticleSystemRenderer>())
            r.sortingOrder = 20;
        Destroy(fx, 2f);
    }

    protected override void Die()
    {
        BossHealthBarUI.Instance?.Unbind(this);

        // Reward shower: XP orbs + coins around the wreckage
        int orbCount = 6;
        int perOrb = Mathf.Max(1, xpDrop / orbCount);
        for (int i = 0; i < orbCount; i++)
            SpawnCollectible<XPOrb>(perOrb);
        for (int i = 0; i < 4; i++)
            SpawnCollectible<Coin>(1);

        SpawnSlamEffect(transform.position);
        GameManager.Instance?.AddKill();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Clean up any attack visuals if the boss dies mid-attack
        foreach (var fx in attackFx)
        {
            if (fx != null) Destroy(fx);
        }
        attackFx.Clear();
        if (BossHealthBarUI.Instance != null)
            BossHealthBarUI.Instance.Unbind(this);
    }
}
