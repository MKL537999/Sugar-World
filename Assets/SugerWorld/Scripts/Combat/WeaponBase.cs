using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public string weaponName = "Weapon";
    public string description = "";
    public Sprite icon;
    public float baseDamage = 10f;
    public float baseCooldown = 1f;
    public int maxLevel = 5;

    [HideInInspector] public int currentLevel = 1;
    [HideInInspector] public float damageMultiplier = 1f;
    [HideInInspector] public float cooldownMultiplier = 1f;

    protected float cooldownTimer;
    protected PlayerController player;
    protected GameConfig config;

    public float CurrentDamage => baseDamage * damageMultiplier * (1f + (currentLevel - 1) * 0.3f);
    public float CurrentCooldown => baseCooldown * cooldownMultiplier * Mathf.Pow(0.9f, currentLevel - 1);

    public virtual void Initialize(PlayerController owner)
    {
        player = owner;
        config = GameManager.Instance?.Config;
        cooldownTimer = 0f;
    }

    public virtual void OnUpdate()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            cooldownTimer = CurrentCooldown;
            Attack();
        }
    }

    protected abstract void Attack();

    public virtual void LevelUp()
    {
        if (currentLevel < maxLevel)
            currentLevel++;
    }
}
