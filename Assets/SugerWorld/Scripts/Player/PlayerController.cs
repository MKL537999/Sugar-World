using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float pickupMagnetSpeed = 8f;
    [SerializeField] private float invincibilityDuration = 0.6f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<WeaponBase> weapons = new List<WeaponBase>();
    [SerializeField] private Transform weaponContainer;

    [Header("Walk Animation Sprites")]
    public Sprite[] walkDown;
    public Sprite[] walkUp;
    public Sprite[] walkSide;

    private const float ArenaHalfSize = 28f;
    private const float WalkFrameRate = 8f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public float PickupRange => pickupRange;
    public float PickupMagnetSpeed => pickupMagnetSpeed;
    public Vector3 MoveDirection { get; private set; }

    private Rigidbody2D rb;
    private Camera mainCam;
    private Vector2 moveInput;
    private Coroutine flashRoutine;
    private Coroutine invRoutine;
    private bool isInvincible;
    private Color originalColor;

    private float animTimer;
    private int animFrame;

    public static PlayerController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        CurrentHealth = maxHealth;
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            var cfg = GameManager.Instance.Config;
            moveSpeed = cfg.playerMoveSpeed;
            maxHealth = cfg.playerMaxHealth;
            pickupRange = cfg.pickupRange;
            pickupMagnetSpeed = cfg.pickupMagnetSpeed;
            invincibilityDuration = cfg.playerInvincibilityTime;
            CurrentHealth = maxHealth;
        }

        foreach (var w in weapons)
        {
            w.Initialize(this);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        MoveDirection = moveInput;

        UpdateAnimation();

        foreach (var w in weapons)
        {
            w.OnUpdate();
        }
    }

    private void UpdateAnimation()
    {
        if (spriteRenderer == null) return;

        bool moving = moveInput.sqrMagnitude > 0.01f;

        if (!moving)
        {
            // Idle: reset to first frame of whichever direction was last used
            // Keep current sprite, just reset anim
            animTimer = 0f;
            animFrame = 0;
            return;
        }

        animTimer += Time.deltaTime;
        if (animTimer >= 1f / WalkFrameRate)
        {
            animTimer = 0f;
            animFrame++;
        }

        Sprite[] currentAnim = null;
        bool flipX = false;

        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            // Horizontal dominant
            currentAnim = walkSide;
            flipX = moveInput.x < 0f; // walkSide sprites face right by default (flipped from left)
            // walkSide is Row0 = left-facing, so flipX=false when moving left, true when moving right
            flipX = moveInput.x > 0f;
        }
        else if (moveInput.y > 0f)
        {
            currentAnim = walkUp;
        }
        else
        {
            currentAnim = walkDown;
        }

        if (currentAnim != null && currentAnim.Length > 0)
        {
            int frame = animFrame % currentAnim.Length;
            spriteRenderer.sprite = currentAnim[frame];
            spriteRenderer.flipX = flipX;
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -ArenaHalfSize, ArenaHalfSize);
        pos.y = Mathf.Clamp(pos.y, -ArenaHalfSize, ArenaHalfSize);
        transform.position = pos;
    }

    private void LateUpdate()
    {
        if (mainCam != null)
        {
            Vector3 target = transform.position;
            target.z = -10f;
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, target, Time.deltaTime * 8f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || isInvincible) return;

        CurrentHealth -= damage;
        if (spriteRenderer != null && flashRoutine == null)
            flashRoutine = StartCoroutine(HitFlash());
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            IsDead = true;
            // Disable collider so enemies stop dealing damage
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = null;
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            GameManager.Instance?.GameOver();
            return;
        }

        // Brief invincibility after every hit so the player cannot be shredded instantly
        StartInvincibility();
    }

    private void StartInvincibility()
    {
        if (invRoutine != null) StopCoroutine(invRoutine);
        invRoutine = StartCoroutine(InvincibilityBlink());
    }

    // While invincible the sprite blinks (alpha flicker) so the state is clearly visible
    private IEnumerator InvincibilityBlink()
    {
        isInvincible = true;
        if (spriteRenderer != null)
        {
            float t = 0f;
            // The red hit-flash coroutine finishes quickly; the blink covers the rest of the window
            while (t < invincibilityDuration)
            {
                t += Time.deltaTime;
                if (flashRoutine == null)
                {
                    // Hard on/off flicker, ~8 Hz — classic i-frame look
                    bool on = Mathf.PingPong(t * 8f, 1f) > 0.5f;
                    float a = on ? 1f : 0.35f;
                    spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, a);
                }
                yield return null;
            }
            spriteRenderer.color = originalColor;
        }
        else
        {
            yield return new WaitForSeconds(invincibilityDuration);
        }
        isInvincible = false;
        invRoutine = null;
    }

    public bool IsInvincible => isInvincible;

    private IEnumerator HitFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.3f, 0.3f);
            yield return new WaitForSecondsRealtime(0.15f);
            spriteRenderer.color = originalColor;
        }
        flashRoutine = null;
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
    }

    public void AddWeapon(WeaponBase weaponPrefab)
    {
        var w = Instantiate(weaponPrefab, weaponContainer);
        w.gameObject.SetActive(true);
        w.Initialize(this);
        weapons.Add(w);
    }

    public int WeaponCount => weapons.Count;

    public int GetWeaponLevel(string weaponName)
    {
        var w = FindWeaponByName(this, weaponName);
        return w != null ? w.currentLevel : 0;
    }

    public static WeaponBase FindWeaponByName(PlayerController player, string weaponName)
    {
        foreach (var w in player.weapons)
        {
            if (w.weaponName == weaponName)
                return w;
        }
        return null;
    }

    public void UpgradeWeapon(int index)
    {
        if (index < weapons.Count)
        {
            weapons[index].LevelUp();
        }
    }

    public void UpgradeWeaponByName(string weaponName)
    {
        foreach (var w in weapons)
        {
            if (w.weaponName == weaponName)
            {
                w.LevelUp();
                return;
            }
        }
        // Fallback: upgrade first weapon if name not found
        if (weapons.Count > 0)
            weapons[0].LevelUp();
    }

    public void UpgradeRandomWeapon()
    {
        if (weapons.Count > 0)
        {
            int idx = Random.Range(0, weapons.Count);
            weapons[idx].LevelUp();
        }
    }

    public void IncreaseStat(StatType stat, float amount)
    {
        switch (stat)
        {
            case StatType.MoveSpeed:
                moveSpeed += amount;
                break;
            case StatType.MaxHealth:
                float healthGain = maxHealth * amount;
                maxHealth += healthGain;
                CurrentHealth += healthGain;
                break;
            case StatType.PickupRange:
                pickupRange += amount;
                break;
            case StatType.Damage:
                foreach (var w in weapons)
                    w.damageMultiplier += amount;
                break;
            case StatType.Cooldown:
                foreach (var w in weapons)
                    w.cooldownMultiplier = Mathf.Max(0.2f, w.cooldownMultiplier - amount);
                break;
        }
    }

    public Transform GetNearestEnemy()
    {
        var enemies = FindObjectsOfType<Enemy>();
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var e in enemies)
        {
            if (!e.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = e.transform;
            }
        }
        return nearest;
    }

    public Vector2 AimDirection
    {
        get
        {
            var nearest = GetNearestEnemy();
            if (nearest != null)
                return ((Vector2)(nearest.position - transform.position)).normalized;
            return MoveDirection.sqrMagnitude > 0.01f ? MoveDirection : Vector2.right;
        }
    }
}

public enum StatType
{
    MoveSpeed,
    MaxHealth,
    PickupRange,
    Damage,
    Cooldown
}
