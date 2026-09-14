using UnityEngine;

public class WeaponRanged : WeaponBase
{
    public Projectile projectilePrefab;
    public float projectileSpeed = 10f;
    public int projectileCount = 1;
    public float spreadAngle = 15f;
    public bool penetrating;

    private ObjectPool<Projectile> pool;

    private void Awake()
    {
        pool = new ObjectPool<Projectile>(projectilePrefab, 20, transform);
    }

    protected override void Attack()
    {
        Vector2 aimDir = player.AimDirection;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = 0f;
            if (projectileCount > 1)
            {
                angle = Mathf.Lerp(-spreadAngle, spreadAngle, (float)i / (projectileCount - 1));
            }
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * aimDir;

            var proj = pool.Get();
            proj.transform.position = player.transform.position;
            proj.Fire(dir, CurrentDamage, projectileSpeed);
            proj.penetrating = penetrating;
        }
    }
}
