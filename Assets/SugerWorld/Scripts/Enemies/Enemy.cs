using System.Collections;
using UnityEngine;

public enum EnemyAttackType
{
    Melee,   // contact damage
    Ranged,  // shoots projectiles
    Charger  // charges after windup with red glow
}

public class Enemy : MonoBehaviour
{
    public float maxHealth = 30f;
    public float damage = 5f;
    public float moveSpeed = 2f;
    public int xpDrop = 5;
    public int coinDropChance = 15;
    public EnemyAttackType attackType = EnemyAttackType.Melee;
    public SpriteRenderer spriteRenderer;
    public Sprite coinDropSprite;
    public Sprite xpDropSprite;

    public bool IsAlive => currentHealth > 0f;
    public float CurrentHealth => currentHealth;

    private float currentHealth;
    private Coroutine flashRoutine;
    private Color originalColor;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    public void Init(float healthScale, float speedScale, float damageScale)
    {
        currentHealth = maxHealth * healthScale;
        moveSpeed *= speedScale;
        damage *= damageScale;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        currentHealth -= amount;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlash());
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.15f, 0.15f);
            yield return new WaitForSecondsRealtime(0.08f);
            spriteRenderer.color = new Color(1f, 0.5f, 0.5f);
            yield return new WaitForSecondsRealtime(0.07f);
            spriteRenderer.color = originalColor;
        }
        flashRoutine = null;
    }

    protected virtual void Die()
    {
        GameManager.Instance?.AddKill();
        SpawnCollectible<XPOrb>(xpDrop);
        if (Random.Range(0, 100) < coinDropChance)
            SpawnCollectible<Coin>(1);
        Destroy(gameObject);
    }

    protected void SpawnCollectible<T>(int value) where T : Component
    {
        var go = new GameObject(typeof(T).Name);
        go.transform.position = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        go.transform.localScale = Vector3.one * 0.3f;

        if (typeof(T) == typeof(XPOrb))
        {
            var orb = go.AddComponent<XPOrb>();
            orb.xpAmount = value;
            sr.sprite = xpDropSprite;
            sr.color = Color.white;
        }
        else if (typeof(T) == typeof(Coin))
        {
            go.AddComponent<Coin>();
            sr.sprite = coinDropSprite;
            sr.color = Color.white;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (attackType == EnemyAttackType.Ranged) return;
        if (other.TryGetComponent<PlayerController>(out var player))
        {
            player.TakeDamage(damage * Time.deltaTime * 2f);
        }
    }
}
