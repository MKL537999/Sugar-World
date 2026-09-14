using System.Collections.Generic;
using UnityEngine;

public class WeaponBoomerang : WeaponBase
{
    public float range = 8f;
    public float speed = 18f;
    public float hitRadius = 1f;
    public GameObject visual;

    [Header("Evolution — Multi Boomerang")]
    public int throwCount = 1;   // 3/5/7... after each evolution pick

    private enum State { Idle, Outbound, Inbound }
    private State state = State.Idle;
    private Vector2 launchDir;
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    private const float HitCheckInterval = 0.05f;
    private float hitTimer;

    // First pick throws 3 boomerangs; every re-pick adds 2 more
    public void EvolveMulti()
    {
        throwCount = throwCount == 1 ? 3 : throwCount + 2;
    }

    public override void Initialize(PlayerController owner)
    {
        base.Initialize(owner);
        if (config != null)
        {
            baseDamage = config.boomerangBaseDamage;
            baseCooldown = config.boomerangBaseCooldown;
            range = config.boomerangRange;
            speed = config.boomerangSpeed;
            hitRadius = config.boomerangHitRadius;
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
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    var target = player.GetNearestEnemy();
                    if (target != null)
                    {
                        if (throwCount > 1)
                        {
                            // Evolved: throw several independent boomerangs evenly around the aim
                            LaunchMulti(target);
                            cooldownTimer = CurrentCooldown;
                        }
                        else
                        {
                            launchDir = ((Vector2)(target.position - player.transform.position)).normalized;
                            hitEnemies.Clear();
                            state = State.Outbound;
                            hitTimer = 0f;
                        }
                    }
                    else
                    {
                        cooldownTimer = CurrentCooldown;
                    }
                }
                break;

            case State.Outbound:
                // Travel outward along launch direction
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    (Vector2)transform.position + launchDir * speed * Time.deltaTime,
                    speed * Time.deltaTime);
                CheckHits();

                if (Vector2.Distance(transform.position, player.transform.position) >= range)
                    state = State.Inbound;
                break;

            case State.Inbound:
                // Track player's current position so the boomerang always returns to the player
                Vector2 playerPos = player.transform.position;
                transform.position = Vector2.MoveTowards(transform.position, playerPos, speed * Time.deltaTime);
                CheckHits();

                if (Vector2.Distance(transform.position, playerPos) < 0.4f)
                {
                    transform.position = playerPos;
                    state = State.Idle;
                    cooldownTimer = CurrentCooldown;
                }
                break;
        }

        // Spin visual
        if (visual != null && state != State.Idle)
            visual.transform.Rotate(0f, 0f, 720f * Time.deltaTime);
    }

    private void CheckHits()
    {
        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0f)
        {
            hitTimer = HitCheckInterval;
            var hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Enemy>(out var enemy) && enemy.IsAlive && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);
                    enemy.TakeDamage(CurrentDamage);
                }
            }
        }
    }

    // Evolved volley: independent boomerangs evenly spaced around the first aim direction
    private void LaunchMulti(Transform target)
    {
        Vector2 aim = ((Vector2)(target.position - player.transform.position)).normalized;
        for (int i = 0; i < throwCount; i++)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, i * (360f / throwCount)) * aim;
            var go = new GameObject("BoomerangFlight");
            var flight = go.AddComponent<BoomerangFlight>();
            flight.Launch(this, dir, player.transform, CurrentDamage);
        }
    }

    // One evolved boomerang: flies out, then homes back to the player, spinning and damaging
    private class BoomerangFlight : MonoBehaviour
    {
        private Vector2 dir;
        private float speed;
        private float range;
        private float hitRadius;
        private float damage;
        private Transform player;
        private bool inbound;
        private float hitTimer;
        private readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

        private const float HitCheckInterval = 0.05f;

        public void Launch(WeaponBoomerang owner, Vector2 direction, Transform ownerPlayer, float dmg)
        {
            dir = direction;
            speed = owner.speed;
            range = owner.range;
            hitRadius = owner.hitRadius;
            damage = dmg;
            player = ownerPlayer;
            transform.position = ownerPlayer.position;

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
            if (!inbound)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    (Vector2)transform.position + dir * speed * Time.deltaTime,
                    speed * Time.deltaTime);
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
                if (Vector2.Distance(transform.position, player.position) < 0.4f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            foreach (var r in GetComponentsInChildren<SpriteRenderer>())
                r.transform.Rotate(0f, 0f, 720f * Time.deltaTime);

            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                hitTimer = HitCheckInterval;
                var hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<Enemy>(out var enemy) && enemy.IsAlive && !hitEnemies.Contains(enemy))
                    {
                        hitEnemies.Add(enemy);
                        enemy.TakeDamage(damage);
                    }
                }
            }

            if (!inbound && Vector2.Distance(transform.position, player.position) >= range)
                inbound = true;
        }
    }

    protected override void Attack()
    {
        // Handled in OnUpdate
    }
}
