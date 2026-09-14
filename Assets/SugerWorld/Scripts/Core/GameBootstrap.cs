using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameBootstrap : MonoBehaviour
{
    [Header("Player Sprite")]
    public Sprite marshalSprite;

    [Header("Enemy Sprites")]
    public List<Sprite> enemySprites = new List<Sprite>();

    [Header("Weapon Sprite")]
    public Sprite forkSprite;
    public Sprite boomerangSprite;
    public Sprite jellyBeanSprite;
    public Sprite bombSprite;

    [Header("Floor Sprite (Tiled)")]
    public Sprite floorSprite;

    [Header("UI Sprites")]
    public Sprite healthBarFillSprite;
    public Sprite healthBarFrameSprite;
    public Sprite xpBarFrameSprite;
    public Sprite coinIconSprite;
    public Sprite portraitSprite;

    [Header("Map Decoration Sprites")]
    public List<Sprite> decorationSprites = new List<Sprite>();

    [Header("Collectible Sprites")]
    public Sprite coinDropSprite;
    public Sprite xpDropSprite;
    public Sprite healthPackSprite;

    [Header("Config")]
    public GameConfig gameConfig;

    private GameManager gameManager;
    private PlayerController player;
    private TMP_Text bossWarningLabel;
    private TMP_Text merchantNoticeLabel;
    private Sprite[] bossSheetCache;
    private Sprite[] merchantSheetCache;

    private void Awake()
    {
        BuildScene();
    }

    private void BuildScene()
    {
        SetupCamera();
        CreateArenaFloor();
        CreateMapDecorations();
        CreateGameManager();
        CreatePlayer();
        CreateEnemySpawner();
        CreateHealthPackSpawner();
        CreateUI();
        CreateBossSystem();
        CreateMerchantSystem();
    }

    // ---- Camera ----
    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(0.45f, 0.32f, 0.55f);
    }

    // ---- Arena Floor ----
    private void CreateArenaFloor()
    {
        var go = new GameObject("ArenaFloor");
        go.transform.position = Vector3.zero;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -10;
        sr.enabled = false;

        if (floorSprite != null)
        {
            sr.sprite = floorSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(60f, 60f);
        }
        else
        {
            sr.color = new Color(0.25f, 0.18f, 0.2f);
            go.transform.localScale = new Vector3(60f, 60f, 1f);
        }
    }

    // ---- Map Decorations ----
    private void CreateMapDecorations()
    {
        if (decorationSprites == null || decorationSprites.Count == 0) return;

        var parent = new GameObject("MapDecorations").transform;
        float minDist = 6f;
        float mapHalfSize = 24f;

        // Keep every decoration inside the actual arena bounds, not beyond the map edges.
        // Trees remain the only tall props; houses are intentionally excluded.
        var tallSprites = new List<Sprite>();
        var groundSprites = new List<Sprite>();
        var shadowSprite = FindDecorationShadow();

        foreach (var s in decorationSprites)
        {
            if (s == null) continue;
            string name = s.name.ToLower();
            if (name.Contains("shadow"))
                continue;
            if (name.Contains("house"))
                continue; // Exclude house buildings entirely.
            if (name.Contains("tree"))
                tallSprites.Add(s);
            else
                groundSprites.Add(s);
        }

        int tallCount = tallSprites.Count > 0 ? 12 : 0;
        float tallMinDist = 16f;
        for (int i = 0; i < tallCount; i++)
        {
            Vector3 pos;
            int attempts = 0;
            do
            {
                pos = new Vector3(
                    Random.Range(-mapHalfSize, mapHalfSize),
                    Random.Range(-mapHalfSize, mapHalfSize),
                    0f);
                attempts++;
            } while ((pos.magnitude < tallMinDist || Mathf.Abs(pos.x) > mapHalfSize || Mathf.Abs(pos.y) > mapHalfSize) && attempts < 30);

            if (pos.magnitude < tallMinDist)
                continue;

            var sprite = tallSprites[Random.Range(0, tallSprites.Count)];
            float scale = Random.Range(1.5f, 2.5f);

            if (shadowSprite != null)
                SpawnDecorationSprite(parent, shadowSprite, pos, scale * 0.8f, -5, new Color(0f, 0f, 0f, 0.3f));

            SpawnDecorationSprite(parent, sprite, pos, scale, -5, new Color(1f, 1f, 1f, 0.9f));
        }

        int groundCount = 30;
        for (int i = 0; i < groundCount; i++)
        {
            Vector3 pos;
            int attempts = 0;
            do
            {
                pos = new Vector3(
                    Random.Range(-mapHalfSize, mapHalfSize),
                    Random.Range(-mapHalfSize, mapHalfSize),
                    0f);
                attempts++;
            } while ((pos.magnitude < minDist || Mathf.Abs(pos.x) > mapHalfSize || Mathf.Abs(pos.y) > mapHalfSize) && attempts < 10);

            if (pos.magnitude < minDist)
                continue;

            var sprite = groundSprites[Random.Range(0, groundSprites.Count)];
            float scale = Random.Range(0.5f, 1.0f);
            SpawnDecorationSprite(parent, sprite, pos, scale, -5, new Color(1f, 1f, 1f, 0.85f));
        }
    }

    private Sprite FindDecorationShadow()
    {
        foreach (var s in decorationSprites)
        {
            if (s != null && s.name.ToLower().Contains("shadow"))
                return s;
        }
        return null;
    }

    private void SpawnDecorationSprite(Transform parent, Sprite sprite, Vector3 pos, float scale, int sortingOrder, Color tint)
    {
        var go = new GameObject($"Decor_{sprite.name}");
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.color = tint;
    }

    // ---- GameManager ----
    private void CreateGameManager()
    {
        var go = new GameObject("GameManager");
        gameManager = go.AddComponent<GameManager>();
        if (gameConfig == null)
            gameConfig = ScriptableObject.CreateInstance<GameConfig>();
        SetPrivateField(gameManager, "config", gameConfig);
    }

    // ---- Player ----
    private void CreatePlayer()
    {
        var go = new GameObject("Player");
        go.transform.position = Vector3.zero;
        go.tag = "Player";

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 8f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        if (marshalSprite != null) sr.sprite = marshalSprite;

        player = go.AddComponent<PlayerController>();
        SetPrivateField(player, "spriteRenderer", sr);

        // Load walk animation sprites from the sprite sheet
        AssignWalkSprites(player);

        var wc = new GameObject("Weapons");
        wc.transform.SetParent(go.transform);
        SetPrivateField(player, "weaponContainer", wc.transform);

        CreateForkWeapon(wc.transform);
    }

    private void CreateForkWeapon(Transform parent)
    {
        var go = new GameObject("ForkWeapon");
        go.transform.SetParent(parent);

        var fork = go.AddComponent<WeaponFork>();
        fork.weaponName = "Fork";
        fork.baseDamage = 15f;
        fork.baseCooldown = 0.8f;
        fork.attackRange = 5f;
        fork.lungeSpeed = 25f;

        var visual = new GameObject("ForkVisual");
        visual.transform.SetParent(go.transform);
        var vsr = visual.AddComponent<SpriteRenderer>();
        vsr.sortingOrder = 5;
        if (forkSprite != null) vsr.sprite = forkSprite;
        visual.transform.localScale = Vector3.one * 0.7f;
        fork.forkVisual = visual;
        fork.icon = forkSprite;

        var weapons = GetPrivateField<List<WeaponBase>>(player, "weapons");
        weapons.Add(fork);
    }

    private void AssignWalkSprites(PlayerController player)
    {
        // MarshalSpriteSheet is 4 cols × 6 rows, 256×256 each
        // Row0 (top): walk left,  Row1: walk up,  Row2: walk down
        // Right = left flipped horizontally
        var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/SugerWorld/Art/Characters/MarshalSpriteSheet.png");
        var spriteList = new List<Sprite>();
        foreach (var s in sprites)
        {
            if (s is Sprite sp)
                spriteList.Add(sp);
        }
        // Sort by Y descending, then X ascending (row-by-row top to bottom)
        spriteList.Sort((a, b) =>
        {
            int rowA = Mathf.FloorToInt(a.rect.y / 256);
            int rowB = Mathf.FloorToInt(b.rect.y / 256);
            if (rowA != rowB) return rowB.CompareTo(rowA);
            return a.rect.x.CompareTo(b.rect.x);
        });

        if (spriteList.Count >= 12)
        {
            player.walkSide = new Sprite[] { spriteList[0], spriteList[1], spriteList[2], spriteList[3] }; // Row0 = left
            player.walkUp   = new Sprite[] { spriteList[4], spriteList[5], spriteList[6], spriteList[7] }; // Row1 = up
            player.walkDown = new Sprite[] { spriteList[8], spriteList[9], spriteList[10], spriteList[11] }; // Row2 = down
        }
    }

    // ---- Weapon Prefab Factories (used by NewWeapon upgrades) ----
    public WeaponBoomerang CreateBoomerangPrefab()
    {
        var go = new GameObject("BoomerangWeapon");
        var w = go.AddComponent<WeaponBoomerang>();
        w.weaponName = "Donut Boomerang";
        w.baseDamage = gameConfig != null ? gameConfig.boomerangBaseDamage : 12f;
        w.baseCooldown = gameConfig != null ? gameConfig.boomerangBaseCooldown : 1.2f;
        w.range = gameConfig != null ? gameConfig.boomerangRange : 8f;
        w.speed = gameConfig != null ? gameConfig.boomerangSpeed : 18f;
        w.hitRadius = gameConfig != null ? gameConfig.boomerangHitRadius : 1f;

        var vis = new GameObject("BoomerangVisual");
        vis.transform.SetParent(go.transform);
        var vsr = vis.AddComponent<SpriteRenderer>();
        vsr.sortingOrder = 5;
        if (boomerangSprite != null) vsr.sprite = boomerangSprite;
        vis.transform.localScale = Vector3.one * 0.6f;
        w.visual = vis;
        w.icon = boomerangSprite;

        go.SetActive(false);
        return w;
    }

    public WeaponJellyBean CreateJellyBeanPrefab()
    {
        var go = new GameObject("JellyBeanWeapon");
        var w = go.AddComponent<WeaponJellyBean>();
        w.weaponName = "Jelly Bean Blaster";
        w.baseDamage = gameConfig != null ? gameConfig.jellyBeanBaseDamage : 6f;
        w.baseCooldown = gameConfig != null ? gameConfig.jellyBeanBaseCooldown : 0.3f;
        w.projectileSpeed = gameConfig != null ? gameConfig.jellyBeanSpeed : 12f;
        w.projectileCount = gameConfig != null ? gameConfig.jellyBeanCount : 1;
        w.spreadAngle = gameConfig != null ? gameConfig.jellyBeanSpread : 10f;
        w.projectileSprite = jellyBeanSprite;
        w.icon = jellyBeanSprite;

        go.SetActive(false);
        return w;
    }

    public WeaponBomb CreateBombPrefab()
    {
        var go = new GameObject("BombWeapon");
        var w = go.AddComponent<WeaponBomb>();
        w.weaponName = "Gulaab Bomb";
        w.baseDamage = gameConfig != null ? gameConfig.bombBaseDamage : 40f;
        w.baseCooldown = gameConfig != null ? gameConfig.bombBaseCooldown : 3f;
        w.throwRange = gameConfig != null ? gameConfig.bombRange : 6f;
        w.throwSpeed = gameConfig != null ? gameConfig.bombThrowSpeed : 8f;
        w.explosionRadius = gameConfig != null ? gameConfig.bombExplosionRadius : 3f;

        var vis = new GameObject("BombVisual");
        vis.transform.SetParent(go.transform);
        var vsr = vis.AddComponent<SpriteRenderer>();
        vsr.sortingOrder = 5;
        if (bombSprite != null) vsr.sprite = bombSprite;
        vis.transform.localScale = Vector3.one * 1.2f;
        w.visual = vis;
        w.icon = bombSprite;

        // Assign explosion effect prefab
        var explosionPath = "Assets/TJGeneratorLibEffects/Prefabs 2D/Explosions/SparkExplosion2D.prefab";
        var explosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(explosionPath);
        if (explosionPrefab != null)
            w.explosionEffectPrefab = explosionPrefab;
        else
            Debug.LogWarning("[GameBootstrap] SparkExplosion2D prefab not found at " + explosionPath);

        go.SetActive(false);
        return w;
    }

    // ---- Boss ----
    // Loads the vending-machine robot sprite sheet (1024x1280, 256px frames),
    // sorted top-to-bottom then left-to-right:
    // [0..3] back view, [4..7] front view, [8..11] side view,
    // [12..15] pose/turnaround row, [16..18] enraged row (dome glowing yellow)
    private Sprite[] LoadBossSheet()
    {
        if (bossSheetCache != null) return bossSheetCache;

        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/SugerWorld/Art/Characters/VendingMachineSpriteSheet.png");
        var list = new List<Sprite>();
        foreach (var a in assets)
        {
            if (a is Sprite s) list.Add(s);
        }
        // Sort by raw rect.y (not row index) so frames from different bottom rows keep a stable order
        list.Sort((a, b) =>
        {
            if (!Mathf.Approximately(a.rect.y, b.rect.y)) return b.rect.y.CompareTo(a.rect.y);
            return a.rect.x.CompareTo(b.rect.x);
        });
        bossSheetCache = list.ToArray();
        return bossSheetCache;
    }

    private void CreateBossSystem()
    {
        var go = new GameObject("BossSpawner");
        var spawner = go.AddComponent<BossSpawner>();
        if (gameConfig != null)
        {
            spawner.spawnInterval = gameConfig.bossSpawnInterval;
            spawner.warningLead = gameConfig.bossWarningLead;
            spawner.healthScalePerSpawn = gameConfig.bossHealthScalePerSpawn;
        }
        spawner.bossTemplate = CreateBossTemplate(go.transform);
        spawner.spawnLocation = new Vector3(0f, 12f, 0f);
        spawner.warningLabel = bossWarningLabel;
    }

    // ---- Merchant (ginger bear) ----
    // Loads the ginger-bear sheet: a single top row of 3 sliced 256px idle frames
    private Sprite[] LoadMerchantSheet()
    {
        if (merchantSheetCache != null) return merchantSheetCache;

        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/SugerWorld/Art/Characters/GingerBearSpriteSheet.png");
        var list = new List<Sprite>();
        foreach (var a in assets)
        {
            if (a is Sprite s) list.Add(s);
        }
        list.Sort((a, b) =>
        {
            if (!Mathf.Approximately(a.rect.y, b.rect.y)) return b.rect.y.CompareTo(a.rect.y);
            return a.rect.x.CompareTo(b.rect.x);
        });
        merchantSheetCache = list.ToArray();
        return merchantSheetCache;
    }

    private void CreateMerchantSystem()
    {
        var go = new GameObject("MerchantSpawner");
        var spawner = go.AddComponent<MerchantSpawner>();
        if (gameConfig != null)
            spawner.spawnInterval = gameConfig.merchantSpawnInterval;
        spawner.merchantTemplate = CreateMerchantTemplate(go.transform);
        spawner.noticeLabel = merchantNoticeLabel;
    }

    private Merchant CreateMerchantTemplate(Transform parent)
    {
        var go = new GameObject("MerchantTemplate");
        go.transform.SetParent(parent);
        go.SetActive(false);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 4f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.6f;
        col.isTrigger = true;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;

        var m = go.AddComponent<Merchant>();
        // Ranged type disables contact damage — the merchant never attacks
        m.attackType = EnemyAttackType.Ranged;
        m.maxHealth = gameConfig != null ? gameConfig.merchantHealth : 90f;
        m.moveSpeed = gameConfig != null ? gameConfig.merchantMoveSpeed : 3.5f;
        m.xpDrop = 0;
        m.coinDropChance = 0;

        var frames = LoadMerchantSheet();
        if (frames.Length >= 3)
        {
            m.idleSprites = new Sprite[] { frames[0], frames[1], frames[2] };
            sr.sprite = frames[0];
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] GingerBearSpriteSheet frames not found; merchant will be invisible");
        }

        go.transform.localScale = Vector3.one * 0.8f;
        return m;
    }

    private Boss CreateBossTemplate(Transform parent)
    {
        var go = new GameObject("BossTemplate");
        go.transform.SetParent(parent);
        go.SetActive(false);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 4f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 1.4f;
        col.isTrigger = true;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;

        var boss = go.AddComponent<Boss>();
        // Ranged type disables the inherited contact damage; the boss hurts via its patterns
        boss.attackType = EnemyAttackType.Ranged;
        boss.maxHealth = gameConfig != null ? gameConfig.bossBaseHealth : 2000f;
        boss.xpDrop = gameConfig != null ? gameConfig.bossXpDrop : 120;
        boss.coinDropChance = 100;
        boss.coinDropSprite = coinDropSprite;
        boss.xpDropSprite = xpDropSprite;
        boss.moveSpeed = gameConfig != null ? gameConfig.bossMoveSpeed : 1.6f;
        boss.laserDamage = gameConfig != null ? gameConfig.bossLaserDamage : 12f;
        boss.projectileDamage = gameConfig != null ? gameConfig.bossProjectileDamage : 9f;
        boss.projectileSpeed = 6f;
        boss.slamDamage = gameConfig != null ? gameConfig.bossSlamDamage : 22f;

        var sheet = LoadBossSheet();
        if (sheet.Length >= 16)
        {
            boss.idleSprites = new Sprite[] { sheet[4], sheet[5], sheet[6], sheet[7] };       // front view
            // Enraged frames are the yellow-dome pose at the end of the pose row plus the
            // bottom enraged row; fall back to the pose row when the sheet is not fully sliced
            boss.attackSprites = sheet.Length >= 19
                ? new Sprite[] { sheet[15], sheet[16], sheet[17], sheet[18] }
                : new Sprite[] { sheet[12], sheet[13], sheet[14], sheet[15] };
            sr.sprite = sheet[4];
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] VendingMachineSpriteSheet frames not found; boss will be invisible");
        }

        var explosionPath = "Assets/TJGeneratorLibEffects/Prefabs 2D/Explosions/SparkExplosion2D.prefab";
        boss.explosionEffectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(explosionPath);

        go.transform.localScale = Vector3.one * 2.2f;
        return boss;
    }

    private void CreateBossUI(Transform parent)
    {
        // ---- Boss health bar (top-center, below the timer) ----
        var barGo = new GameObject("BossHealthBar");
        barGo.transform.SetParent(parent);
        SetAnchor(barGo, new Vector2(0.33f, 0.855f), new Vector2(0.67f, 0.925f));

        var bar = barGo.AddComponent<BossHealthBarUI>();
        var frameImg = barGo.AddComponent<Image>();
        var frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SugerWorld/Art/UI/UIHealthFrameBlue.png");
        if (frameSprite != null)
        {
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Simple;
        }
        else
        {
            frameImg.color = new Color(0.12f, 0.08f, 0.16f, 0.95f);
        }

        // Boss face in the frame's portrait slot (mirrors the player health bar layout)
        var sheet = LoadBossSheet();
        if (sheet.Length > 4)
        {
            var portraitGo = new GameObject("BossPortrait");
            portraitGo.transform.SetParent(barGo.transform);
            SetAnchor(portraitGo, new Vector2(0.01f, 0.05f), new Vector2(0.40f, 0.95f));
            var portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.sprite = sheet[4];
            portraitImg.preserveAspect = true;
        }

        // Fill — the same pink bar sprite as the player's, tinted red for the boss
        var fillGo = new GameObject("BossFill");
        fillGo.transform.SetParent(barGo.transform);
        SetAnchor(fillGo, new Vector2(0.42f, 0.28f), new Vector2(0.97f, 0.72f));
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.raycastTarget = false;
        var fillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SugerWorld/Art/UI/UIHealthBarPink.png");
        if (fillSprite != null)
        {
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            fillImg.color = new Color(1f, 0.35f, 0.3f);
        }
        else
        {
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            fillImg.color = new Color(0.85f, 0.2f, 0.2f);
        }

        var label = CreateLabel(barGo.transform, "VEND-O-MATIC", 14, Color.white,
            new Vector2(0.42f, 0.28f), new Vector2(0.97f, 0.72f));

        bar.panel = barGo;
        bar.fill = fillImg;
        bar.label = label;
        barGo.SetActive(false);

        // ---- Boss warning banner (center screen) ----
        var banner = CreateLabel(parent, "!! BOSS INCOMING !!", 36, new Color(1f, 0.25f, 0.2f),
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.80f));
        banner.gameObject.SetActive(false);
        bossWarningLabel = banner;
    }

    // ---- Enemy Spawner ----
    private void CreateEnemySpawner()
    {
        var go = new GameObject("EnemySpawner");
        var spawner = go.AddComponent<EnemySpawner>();

        var prefabs = new List<Enemy>();

        foreach (var sprite in enemySprites)
        {
            var enemy = CreateEnemyTemplate(sprite, spawner.transform);
            prefabs.Add(enemy);
        }

        if (prefabs.Count == 0)
        {
            // Fallback: create a simple colored square enemy
            var enemy = CreateEnemyTemplate(null, spawner.transform);
            prefabs.Add(enemy);
        }

        SetPrivateField(spawner, "enemyPrefabs", prefabs);
    }

    private Enemy CreateEnemyTemplate(Sprite sprite, Transform parent)
    {
        var go = new GameObject($"EnemyTemplate_{sprite?.name ?? "default"}");
        go.SetActive(false);
        go.transform.SetParent(parent);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 4f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;
        col.isTrigger = true;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0;
        if (sprite != null) sr.sprite = sprite;

        var enemy = go.AddComponent<Enemy>();
        enemy.maxHealth = 30f;
        enemy.damage = 5f;
        enemy.moveSpeed = 2f;
        enemy.xpDrop = 5;
        enemy.coinDropSprite = coinDropSprite;
        enemy.xpDropSprite = xpDropSprite;

        // Assign attack type based on sprite for enemy variety
        string spriteName = sprite != null ? sprite.name : "";
        switch (spriteName)
        {
            case "DonutPink":
                // Donut rolls at the player — Charger
                enemy.attackType = EnemyAttackType.Charger;
                enemy.damage = 8f;
                enemy.moveSpeed = 2.5f;
                break;
            case "DonutYellow":
                // Yellow donut also charges
                enemy.attackType = EnemyAttackType.Charger;
                enemy.damage = 8f;
                enemy.moveSpeed = 2.2f;
                break;
            case "CakeChocBall":
                // Choc ball lobs projectiles — Ranged (squishier than melee)
                enemy.attackType = EnemyAttackType.Ranged;
                enemy.damage = 3f;
                enemy.moveSpeed = 1.5f;
                enemy.maxHealth = 20f;
                break;
            case "CakeCocoBall":
                // Coco ball also ranged
                enemy.attackType = EnemyAttackType.Ranged;
                enemy.damage = 3f;
                enemy.moveSpeed = 1.7f;
                enemy.maxHealth = 20f;
                break;
            default:
                // Jellies, Jellybeans — Melee contact damage
                enemy.attackType = EnemyAttackType.Melee;
                break;
        }

        go.AddComponent<EnemyMover>();
        return enemy;
    }

    // ---- Health Pack Spawner ----
    private void CreateHealthPackSpawner()
    {
        var go = new GameObject("HealthPackSpawner");
        var spawner = go.AddComponent<HealthPackSpawner>();
        spawner.healthPackSprite = healthPackSprite;
    }

    // ---- UI Canvas ----
    private void CreateUI()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        CreateHUD(canvasGo.transform);
        CreateBossUI(canvasGo.transform);
        CreateMerchantNotice(canvasGo.transform);
        CreateLevelUpPanel(canvasGo.transform);
        CreateGameOverPanel(canvasGo.transform);
        CreateEvolutionShopUI(canvasGo.transform);
        CreateEventSystem();
    }

    // ---- Merchant arrival notice (below the boss banner) ----
    private void CreateMerchantNotice(Transform parent)
    {
        var notice = CreateLabel(parent, "GINGER-BEAR MERCHANT!", 24, new Color(1f, 0.78f, 0.3f),
            new Vector2(0.08f, 0.615f), new Vector2(0.92f, 0.69f));
        notice.gameObject.SetActive(false);
        merchantNoticeLabel = notice;
    }

    // ---- Evolution shop (opened by defeating the merchant) ----
    private void CreateEvolutionShopUI(Transform parent)
    {
        var go = new GameObject("EvolutionShopPanel");
        go.transform.SetParent(parent);
        SetAnchor(go, new Vector2(0.15f, 0.14f), new Vector2(0.85f, 0.84f));
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.08f, 0.14f, 0.96f);

        var shop = go.AddComponent<EvolutionShopUI>();
        SetPrivateField(shop, "panel", go);

        // Title (left) + coin balance (right)
        CreateLabel(go.transform, "EVOLUTION SHOP", 32, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.02f, 0.86f), new Vector2(0.58f, 0.98f));
        var coinLabel = CreateLabel(go.transform, "Coins: 0", 24, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.58f, 0.86f), new Vector2(0.98f, 0.98f));
        coinLabel.alignment = TMPro.TextAlignmentOptions.Right;
        SetPrivateField(shop, "coinLabel", coinLabel);

        // Three option cards
        var options = new List<EvolutionShopUI.ShopOptionUI>();
        for (int i = 0; i < 3; i++)
            options.Add(CreateShopOption(go.transform, i, shop));
        SetPrivateField(shop, "options", options);

        // Close button — the player may leave without buying anything
        var btnGo = new GameObject("CloseButton");
        btnGo.transform.SetParent(go.transform);
        SetAnchor(btnGo, new Vector2(0.32f, 0.02f), new Vector2(0.68f, 0.13f));
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.45f, 0.3f, 0.15f);
        var btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(shop.Close);
        CreateLabel(btnGo.transform, "CLOSE", 20, Color.white, Vector2.zero, Vector2.one);

        go.SetActive(false);
    }

    private EvolutionShopUI.ShopOptionUI CreateShopOption(Transform parent, int index, EvolutionShopUI shop)
    {
        var go = new GameObject($"ShopOption_{index}");
        go.transform.SetParent(parent);
        float y = 0.60f - index * 0.22f;
        SetAnchor(go, new Vector2(0.05f, y), new Vector2(0.95f, y + 0.21f));

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.16f, 0.32f, 1f);
        var button = go.AddComponent<Button>();
        button.onClick.AddListener(() => shop.OnOptionClicked(index));

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform);
        SetAnchor(iconGo, new Vector2(0.02f, 0.08f), new Vector2(0.15f, 0.92f));
        var iconImg = iconGo.AddComponent<Image>();

        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(go.transform);
        SetAnchor(nameGo, new Vector2(0.17f, 0.52f), new Vector2(0.99f, 0.95f));
        var nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
        nameTxt.fontSize = 20;
        nameTxt.color = Color.white;
        nameTxt.alignment = TMPro.TextAlignmentOptions.Left;

        var descGo = new GameObject("Desc");
        descGo.transform.SetParent(go.transform);
        SetAnchor(descGo, new Vector2(0.17f, 0.2f), new Vector2(0.99f, 0.52f));
        var descTxt = descGo.AddComponent<TextMeshProUGUI>();
        descTxt.fontSize = 14;
        descTxt.color = new Color(0.8f, 0.8f, 0.8f);
        descTxt.alignment = TMPro.TextAlignmentOptions.Left;

        var costGo = new GameObject("Cost");
        costGo.transform.SetParent(go.transform);
        SetAnchor(costGo, new Vector2(0.17f, 0.02f), new Vector2(0.6f, 0.2f));
        var costTxt = costGo.AddComponent<TextMeshProUGUI>();
        costTxt.fontSize = 15;
        costTxt.color = new Color(1f, 0.85f, 0.3f);
        costTxt.alignment = TMPro.TextAlignmentOptions.Left;

        return new EvolutionShopUI.ShopOptionUI
        {
            gameObject = go,
            icon = iconImg,
            nameText = nameTxt,
            descriptionText = descTxt,
            costText = costTxt,
            button = button
        };
    }

    private void CreateHUD(Transform parent)
    {
        var go = new GameObject("HUD");
        go.transform.SetParent(parent);
        AddFullRect(go);
        var hud = go.AddComponent<HUD>();

        // ---- Health bar (top-left) ----
        // Frame: stretched (no preserveAspect) so the bar area matches its RectTransform
        var hpFrameGo = new GameObject("HealthFrame");
        hpFrameGo.transform.SetParent(go.transform);
        SetAnchor(hpFrameGo, new Vector2(0.01f, 0.88f), new Vector2(0.28f, 0.99f));
        var hpFrameImg = hpFrameGo.AddComponent<Image>();
        if (healthBarFrameSprite != null)
        {
            hpFrameImg.sprite = healthBarFrameSprite;
            hpFrameImg.type = Image.Type.Simple;
        }
        else
        {
            hpFrameImg.color = new Color(0.15f, 0.12f, 0.2f, 0.9f);
        }

        // Portrait inside the circular area (left ~42% of the frame)
        if (portraitSprite != null)
        {
            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(hpFrameGo.transform);
            SetAnchor(portraitGo, new Vector2(0.01f, 0.05f), new Vector2(0.40f, 0.95f));
            var portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.sprite = portraitSprite;
            portraitImg.type = Image.Type.Simple;
            portraitImg.preserveAspect = true;
        }

        // Health fill — pink sprite, Type.Filled clips horizontally by fillAmount
        // Positioned inside the frame's visible bar area (~42% to ~97% horizontal, ~28% to ~72% vertical)
        var hpFillGo = new GameObject("HealthFill");
        hpFillGo.transform.SetParent(hpFrameGo.transform);
        SetAnchor(hpFillGo, new Vector2(0.42f, 0.28f), new Vector2(0.97f, 0.72f));
        var hpFillImg = hpFillGo.AddComponent<Image>();
        hpFillImg.raycastTarget = false;
        if (healthBarFillSprite != null)
        {
            hpFillImg.sprite = healthBarFillSprite;
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.fillAmount = 1f;
        }
        else
        {
            hpFillImg.color = new Color(0.3f, 0.85f, 0.4f);
        }
        SetPrivateField(hud, "healthBarFill", hpFillImg);

        // Health text overlaid on the bar
        var hpText = CreateLabel(hpFrameGo.transform, "100/100", 14, Color.white,
            new Vector2(0.42f, 0.28f), new Vector2(0.97f, 0.72f));
        SetPrivateField(hud, "healthText", hpText);

        // Level text (below health bar, left-aligned)
        var lvl = CreateLabel(go.transform, "Lv.1", 22, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.02f, 0.83f), new Vector2(0.15f, 0.89f));
        lvl.alignment = TMPro.TextAlignmentOptions.Left;
        SetPrivateField(hud, "levelText", lvl);

        // ---- XP bar (top-right, visually same bar length as health bar) ----
        // HP frame is 0.01-0.28 (27% width) but the visible bar area is ~55% of that (~15%).
        // XP frame: 0.84-0.99 (15% width) to match the visible HP bar length.
        var xpFrameGo = new GameObject("XPFrame");
        xpFrameGo.transform.SetParent(go.transform);
        SetAnchor(xpFrameGo, new Vector2(0.84f, 0.91f), new Vector2(0.99f, 0.98f));
        var xpFrameImg = xpFrameGo.AddComponent<Image>();
        if (xpBarFrameSprite != null)
        {
            xpFrameImg.sprite = xpBarFrameSprite;
            xpFrameImg.type = Image.Type.Simple;
        }
        else
        {
            xpFrameImg.color = new Color(0.12f, 0.1f, 0.18f, 0.92f);
        }

        // XP fill — reuse the pink striped bar sprite tinted gold for visual consistency
        var xpFillGo = new GameObject("XpFill");
        xpFillGo.transform.SetParent(xpFrameGo.transform);
        SetAnchor(xpFillGo, new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.9f));
        var xpFillImg = xpFillGo.AddComponent<Image>();
        xpFillImg.raycastTarget = false;
        if (healthBarFillSprite != null)
        {
            xpFillImg.sprite = healthBarFillSprite;
            xpFillImg.type = Image.Type.Filled;
            xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            xpFillImg.fillAmount = 0f;
            xpFillImg.color = new Color(1f, 0.82f, 0.2f);
        }
        else
        {
            xpFillImg.color = new Color(0.95f, 0.75f, 0.2f);
            xpFillImg.type = Image.Type.Filled;
            xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            xpFillImg.fillAmount = 0f;
        }
        SetPrivateField(hud, "xpBarFill", xpFillImg);

        // XP text overlaid on the bar — same size and color as health text
        var xpLabel = CreateLabel(xpFrameGo.transform, "0/20", 14, new Color(0.3f, 0.6f, 0.9f, 1f),
            new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.9f));
        SetPrivateField(hud, "xpText", xpLabel);

        // ---- Timer (top-center) ----
        var time = CreateLabel(go.transform, "00:00", 28, Color.white,
            new Vector2(0.42f, 0.93f), new Vector2(0.58f, 0.99f));
        SetPrivateField(hud, "timeText", time);

        // ---- Coins (below XP bar, right-aligned) ----
        var coinContainer = new GameObject("CoinDisplay");
        coinContainer.transform.SetParent(go.transform);
        SetAnchor(coinContainer, new Vector2(0.84f, 0.86f), new Vector2(0.99f, 0.90f));

        if (coinIconSprite != null)
        {
            var iconGo = new GameObject("CoinIcon");
            iconGo.transform.SetParent(coinContainer.transform);
            SetAnchor(iconGo, new Vector2(0f, 0f), new Vector2(0.2f, 1f));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = coinIconSprite;
            iconImg.preserveAspect = true;
        }

        var coin = CreateLabel(coinContainer.transform, "0", 18, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.22f, 0f), new Vector2(1f, 1f));
        coin.alignment = TMPro.TextAlignmentOptions.Left;
        SetPrivateField(hud, "coinText", coin);

        // ---- Kills (below coins, right-aligned) ----
        var kills = CreateLabel(go.transform, "Kills: 0", 14, new Color(0.9f, 0.9f, 0.9f),
            new Vector2(0.84f, 0.82f), new Vector2(0.99f, 0.86f));
        kills.alignment = TMPro.TextAlignmentOptions.Left;
        SetPrivateField(hud, "killText", kills);
    }

    private TMP_Text CreateLabel(Transform parent, string text, int fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent);
        SetAnchor(go, anchorMin, anchorMax);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        return txt;
    }

    private void CreateLevelUpPanel(Transform parent)
    {
        var go = new GameObject("LevelUpPanel");
        go.transform.SetParent(parent);
        SetAnchor(go, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.8f));
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.08f, 0.15f, 0.95f);

        var levelUp = go.AddComponent<LevelUpUI>();
        SetPrivateField(levelUp, "panel", go);

        // Title
        CreateLabel(go.transform, "LEVEL UP!", 36, new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0.85f), new Vector2(1f, 0.98f));

        // Options
        var options = new List<LevelUpUI.UpgradeOptionUI>();
        for (int i = 0; i < 3; i++)
        {
            var opt = CreateUpgradeOption(go.transform, i);
            options.Add(opt);
        }
        SetPrivateField(levelUp, "options", options);

        // Upgrade pool
        var pool = new List<UpgradeData>();
        pool.Add(MakeUpgrade("Speed Up", "Move faster", UpgradeType.StatBoost, StatType.MoveSpeed, 0.8f, "0.8"));
        pool.Add(MakeUpgrade("Vitality", "+15% max health", UpgradeType.StatBoost, StatType.MaxHealth, 0.15f, "15%"));
        pool.Add(MakeUpgrade("Magnet", "Bigger pickup range", UpgradeType.StatBoost, StatType.PickupRange, 1f, "1.0"));
        pool.Add(MakeUpgrade("Power Up", "All weapons +30% damage", UpgradeType.StatBoost, StatType.Damage, 0.3f, "30%"));
        pool.Add(MakeUpgrade("Haste", "All weapons -15% cooldown", UpgradeType.StatBoost, StatType.Cooldown, 0.15f, "15%"));
        var forkUpgrade = MakeUpgrade("Fork Upgrade", "Upgrade fork weapon", UpgradeType.WeaponUpgrade, StatType.Damage, 0f, "");
        forkUpgrade.weaponUpgradeTarget = "Fork";
        pool.Add(forkUpgrade);

        // New weapon unlocks at milestone levels
        var boomerangPrefab = CreateBoomerangPrefab();
        boomerangPrefab.transform.SetParent(go.transform);
        var boomerangUpgrade = MakeNewWeaponUpgrade("Donut Boomerang", "Throws a spinning donut that returns", boomerangPrefab, 5);
        pool.Add(boomerangUpgrade);

        var jellyBeanPrefab = CreateJellyBeanPrefab();
        jellyBeanPrefab.transform.SetParent(go.transform);
        var jellyBeanUpgrade = MakeNewWeaponUpgrade("Jelly Bean Blaster", "Rapid-fires jelly beans at enemies", jellyBeanPrefab, 5);
        pool.Add(jellyBeanUpgrade);

        var bombPrefab = CreateBombPrefab();
        bombPrefab.transform.SetParent(go.transform);
        var bombUpgrade = MakeNewWeaponUpgrade("Gulaab Bomb", "Lobs an explosive dealing area damage", bombPrefab, 10);
        pool.Add(bombUpgrade);

        SetPrivateField(levelUp, "upgradePool", pool);

        go.SetActive(false);
    }

    private LevelUpUI.UpgradeOptionUI CreateUpgradeOption(Transform parent, int index)
    {
        var go = new GameObject($"Option_{index}");
        go.transform.SetParent(parent);
        float y = 0.55f - index * 0.2f;
        SetAnchor(go, new Vector2(0.05f, y), new Vector2(0.95f, y + 0.15f));

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.15f, 0.3f, 1f);
        var btn = go.AddComponent<Button>();

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform);
        SetAnchor(iconGo, new Vector2(0.02f, 0.1f), new Vector2(0.15f, 0.9f));
        var iconImg = iconGo.AddComponent<Image>();

        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(go.transform);
        SetAnchor(nameGo, new Vector2(0.18f, 0.5f), new Vector2(0.98f, 0.95f));
        var nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
        nameTxt.fontSize = 16;
        nameTxt.color = Color.white;

        var descGo = new GameObject("Desc");
        descGo.transform.SetParent(go.transform);
        SetAnchor(descGo, new Vector2(0.18f, 0.05f), new Vector2(0.98f, 0.5f));
        var descTxt = descGo.AddComponent<TextMeshProUGUI>();
        descTxt.fontSize = 12;
        descTxt.color = new Color(0.8f, 0.8f, 0.8f);

        return new LevelUpUI.UpgradeOptionUI
        {
            gameObject = go,
            icon = iconImg,
            nameText = nameTxt,
            descriptionText = descTxt,
            button = btn
        };
    }

    private UpgradeData MakeUpgrade(string name, string desc, UpgradeType type, StatType stat, float amount, string display)
    {
        var d = ScriptableObject.CreateInstance<UpgradeData>();
        d.upgradeName = name;
        d.description = desc;
        d.type = type;
        d.statType = stat;
        d.statAmount = amount;
        d.statDisplayValue = display;
        return d;
    }

    private UpgradeData MakeNewWeaponUpgrade(string name, string desc, WeaponBase prefab, int minLevel)
    {
        var d = ScriptableObject.CreateInstance<UpgradeData>();
        d.upgradeName = name;
        d.description = desc;
        d.type = UpgradeType.NewWeapon;
        d.weaponPrefab = prefab;
        d.minLevel = minLevel;
        return d;
    }

    private void CreateGameOverPanel(Transform parent)
    {
        var go = new GameObject("GameOverPanel");
        go.transform.SetParent(parent);
        SetAnchor(go, Vector2.zero, Vector2.one);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        var gameOver = go.AddComponent<GameOverPanel>();
        SetPrivateField(gameOver, "panel", go);

        var results = new GameObject("Results");
        results.transform.SetParent(go.transform);
        SetAnchor(results, new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.7f));

        var title = CreateLabel(results.transform, "GAME OVER", 40, new Color(1f, 0.3f, 0.3f),
            new Vector2(0f, 0.7f), new Vector2(1f, 0.98f));

        var timeTxt = CreateLabel(results.transform, "Survived: 00:00", 20, Color.white,
            new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.62f));
        SetPrivateField(gameOver, "surviveTimeText", timeTxt);

        var killTxt = CreateLabel(results.transform, "Kills: 0", 18, Color.white,
            new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.48f));
        SetPrivateField(gameOver, "killsText", killTxt);

        var coinTxt = CreateLabel(results.transform, "Coins: 0", 18, Color.white,
            new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.36f));
        SetPrivateField(gameOver, "coinsText", coinTxt);

        var lvlTxt = CreateLabel(results.transform, "Level: 1", 18, Color.white,
            new Vector2(0.1f, 0.14f), new Vector2(0.9f, 0.24f));
        SetPrivateField(gameOver, "levelText", lvlTxt);

        // Restart button
        var btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(go.transform);
        SetAnchor(btnGo, new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.18f));
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.8f, 0.3f, 0.3f);
        var btn = btnGo.AddComponent<Button>();
        SetPrivateField(gameOver, "restartButton", btn);

        CreateLabel(btnGo.transform, "RESTART", 24, Color.white, Vector2.zero, Vector2.one);

        go.SetActive(false);
    }

    private void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // ---- Helpers ----
    private static void SetAnchor(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void AddFullRect(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void SetPrivateField(object obj, string name, object value)
    {
        var field = obj.GetType().GetField(name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(obj, value);
    }

    private static T GetPrivateField<T>(object obj, string name)
    {
        var field = obj.GetType().GetField(name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (T)field.GetValue(obj) : default;
    }
}
