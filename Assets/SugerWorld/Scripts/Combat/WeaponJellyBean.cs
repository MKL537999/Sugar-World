using UnityEngine;

public class WeaponJellyBean : WeaponBase
{
    public float projectileSpeed = 12f;
    public int projectileCount = 1;
    public float spreadAngle = 10f;
    public Sprite projectileSprite;

    [Header("Evolution — Shotgun")]
    public bool isShotgun;

    private ObjectPool<Projectile> pool;

    // First pick: 5 pellets across a tight 30° cone. Re-picks widen the spread up
    // to a 180° front arc, add 3 more pellets each, and slow the fire rate.
    public void EvolveShotgun()
    {
        if (!isShotgun)
        {
            isShotgun = true;
            spreadAngle = 30f;
            projectileCount = 5;
            cooldownMultiplier *= 1.2f;
        }
        else
        {
            spreadAngle = Mathf.Min(180f, spreadAngle + 20f);
            projectileCount += 3;
            cooldownMultiplier *= 1.2f;
        }
    }

    public override void Initialize(PlayerController owner)
    {
        base.Initialize(owner);
        if (config != null)
        {
            baseDamage = config.jellyBeanBaseDamage;
            baseCooldown = config.jellyBeanBaseCooldown;
            projectileSpeed = config.jellyBeanSpeed;
            projectileCount = config.jellyBeanCount;
            spreadAngle = config.jellyBeanSpread;
        }
    }

    private void Awake()
    {
        var projGo = new GameObject("JellyBeanProj");
        projGo.transform.SetParent(transform);

        var rb = projGo.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = projGo.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        var sr = projGo.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;
        if (projectileSprite != null) sr.sprite = projectileSprite;
        projGo.transform.localScale = Vector3.one * 0.7f;

        var proj = projGo.AddComponent<Projectile>();
        projGo.SetActive(false);

        pool = new ObjectPool<Projectile>(proj, 30, transform);
    }

    protected override void Attack()
    {
        Vector2 aimDir = player.AimDirection;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = 0f;
            if (projectileCount > 1)
                angle = Mathf.Lerp(-spreadAngle, spreadAngle, (float)i / (projectileCount - 1));

            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * aimDir;
            var proj = pool.Get();
            proj.transform.position = player.transform.position;
            proj.Fire(dir, CurrentDamage, projectileSpeed);
            proj.penetrating = false;
        }
    }
}
