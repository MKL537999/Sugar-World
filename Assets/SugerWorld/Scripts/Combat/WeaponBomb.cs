using System.Collections.Generic;
using UnityEngine;

public class WeaponBomb : WeaponBase
{
    public float throwRange = 7f;
    public float throwSpeed = 8f;
    public float explosionRadius = 4f;
    public GameObject visual;
    public GameObject explosionEffectPrefab;
    // Explosion effect scale = explosionRadius * this multiplier (keeps the visual matched to the AoE)
    public float explosionEffectScale = 0.75f;

    [Header("Evolution — Mortar")]
    public bool isMortar;
    public int mortarCount = 1;   // +1 bomb per volley with each re-pick

    private enum State { Idle, Throwing }
    private State state = State.Idle;
    private Vector2 targetPos;

    private const float HitRadius = 0.8f;
    // Renders above enemies (0), player (1) and weapon visuals (5) so the blast is never hidden
    private const int EffectSortingOrder = 20;

    // First pick switches to mortar mode; re-picks add one bomb per volley
    public void EvolveMortar()
    {
        if (!isMortar)
            isMortar = true;
        else
            mortarCount++;
    }

    public override void Initialize(PlayerController owner)
    {
        base.Initialize(owner);
        if (config != null)
        {
            baseDamage = config.bombBaseDamage;
            baseCooldown = config.bombBaseCooldown;
            throwRange = config.bombRange;
            throwSpeed = config.bombThrowSpeed;
            explosionRadius = config.bombExplosionRadius;
        }
        transform.SetParent(null);
        state = State.Idle;
    }

    public override void OnUpdate()
    {
        if (player == null) return;

        switch (state)
        {
            case State.Idle:
                transform.position = player.transform.position;
                if (visual != null) visual.SetActive(false);

                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    if (isMortar)
                    {
                        // Evolved: shells rain onto the densest enemy cluster — nothing blocks them
                        if (FireMortar())
                            cooldownTimer = CurrentCooldown;
                        else
                            cooldownTimer = CurrentCooldown;
                    }
                    else
                    {
                        var target = FindNearestEnemy();
                        if (target != null)
                        {
                            targetPos = target.position;
                            state = State.Throwing;
                            if (visual != null) visual.SetActive(true);
                        }
                        else
                        {
                            cooldownTimer = CurrentCooldown;
                        }
                    }
                }
                break;

            case State.Throwing:
                transform.position = Vector2.MoveTowards(transform.position, targetPos, throwSpeed * Time.deltaTime);

                // Check for collision with any enemy — explode on first contact
                var hits = Physics2D.OverlapCircleAll(transform.position, HitRadius);
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<Enemy>(out var enemy) && enemy.IsAlive)
                    {
                        Explode();
                        return;
                    }
                }

                // Also explode if we've reached the target position
                if (Vector2.Distance(transform.position, targetPos) < 0.2f)
                {
                    Explode();
                }
                break;
        }
    }

    private void Explode()
    {
        // Spawn explosion effect
        if (explosionEffectPrefab != null)
        {
            var fx = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            // Scale the visual so it covers the actual damage radius
            fx.transform.localScale = Vector3.one * Mathf.Max(1f, explosionRadius * explosionEffectScale);
            // Draw above all gameplay sprites so the blast is clearly visible
            foreach (var r in fx.GetComponentsInChildren<ParticleSystemRenderer>())
                r.sortingOrder = EffectSortingOrder;
            Destroy(fx, 2f);
        }

        // Deal AoE damage
        var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy) && enemy.IsAlive)
            {
                enemy.TakeDamage(CurrentDamage);
            }
        }

        state = State.Idle;
        cooldownTimer = CurrentCooldown;
    }

    private Transform FindNearestEnemy()
    {
        var enemies = FindObjectsOfType<Enemy>();
        Transform nearest = null;
        float minDist = throwRange;
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

    // ---- Evolution: mortar volley ----

    private bool FireMortar()
    {
        var enemies = FindObjectsOfType<Enemy>();
        if (enemies.Length == 0) return false;

        Vector2 cluster = FindDensestCluster(enemies);
        // First shell lands dead-centre on the densest point; the rest form a ring
        // far enough out (2.2x blast radius) that the explosion circles never overlap
        float ringRadius = explosionRadius * 2.2f;
        float ringOffset = Random.Range(0f, 360f);
        for (int i = 0; i < mortarCount; i++)
        {
            Vector2 target;
            if (i == 0 || mortarCount == 1)
            {
                target = cluster;
            }
            else
            {
                float a = (ringOffset + (i - 1) * (360f / (mortarCount - 1))) * Mathf.Deg2Rad;
                target = cluster + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ringRadius;
            }
            var shellGo = new GameObject("MortarShell");
            var shell = shellGo.AddComponent<MortarShell>();
            shell.Launch(this, target, CurrentDamage);
        }
        return true;
    }

    // The enemy with the most neighbours inside one blast radius is the cluster centre
    private Vector2 FindDensestCluster(Enemy[] enemies)
    {
        Vector2 best = player.transform.position;
        int bestCount = 0;
        foreach (var anchor in enemies)
        {
            if (anchor == null || !anchor.IsAlive) continue;
            int count = 0;
            foreach (var other in enemies)
            {
                if (other == null || !other.IsAlive) continue;
                if (Vector2.Distance(anchor.transform.position, other.transform.position) <= explosionRadius)
                    count++;
            }
            if (count > bestCount)
            {
                bestCount = count;
                best = anchor.transform.position;
            }
        }
        return best;
    }

    // One evolved shell: falls straight from the sky onto its target and explodes on impact
    private class MortarShell : MonoBehaviour
    {
        private Vector3 targetPos;
        private float fallSpeed;
        private float radius;
        private float damage;
        private GameObject explosionPrefab;
        private float effectScale;

        private const int EffectSortingOrder = 20;

        public void Launch(WeaponBomb owner, Vector2 target, float dmg)
        {
            targetPos = target;
            fallSpeed = 16f;
            radius = owner.explosionRadius;
            damage = dmg;
            explosionPrefab = owner.explosionEffectPrefab;
            effectScale = owner.explosionEffectScale;
            transform.position = targetPos + new Vector3(0f, 14f, 0f);

            if (owner.visual != null)
            {
                var v = Instantiate(owner.visual);
                v.transform.SetParent(transform);
                v.transform.localPosition = Vector3.zero;
                v.transform.localScale = owner.visual.transform.localScale;
                v.SetActive(true);
                v.name = "Visual";
            }
        }

        private void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, fallSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.05f)
                Explode();
        }

        private void Explode()
        {
            if (explosionPrefab != null)
            {
                var fx = Instantiate(explosionPrefab, targetPos, Quaternion.identity);
                fx.transform.localScale = Vector3.one * Mathf.Max(1f, radius * effectScale);
                foreach (var r in fx.GetComponentsInChildren<ParticleSystemRenderer>())
                    r.sortingOrder = EffectSortingOrder;
                Destroy(fx, 2f);
            }

            var hits = Physics2D.OverlapCircleAll(targetPos, radius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Enemy>(out var enemy) && enemy.IsAlive)
                    enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }

    protected override void Attack()
    {
        // Handled in OnUpdate
    }
}
