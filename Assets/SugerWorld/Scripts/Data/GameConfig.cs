using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "SugarWorld/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Player")]
    public float playerMoveSpeed = 5f;
    public float playerMaxHealth = 30f;
    public float pickupRange = 3f;
    public float pickupMagnetSpeed = 8f;
    public float playerInvincibilityTime = 0.6f;   // i-frames after each hit
    public float healthPackHealPercent = 0.08f;     // fraction of max HP per pack

    [Header("Enemy")]
    public float enemyBaseHealth = 30f;
    public float enemyBaseDamage = 5f;
    public float enemyBaseSpeed = 2f;
    public int enemyBaseXpDrop = 5;
    public float enemyHealthScalePerMinute = 0.3f;
    public float enemySpeedScalePerMinute = 0.1f;
    public float enemyDamageScalePerMinute = 0.2f;

    [Header("Spawning")]
    public float spawnInterval = 1.75f;
    public int spawnsPerWave = 25;
    public float waveDuration = 30f;
    public float spawnDistanceMin = 10f;
    public float spawnDistanceMax = 15f;
    public int maxEnemiesAlive = 150;
    public float spawnIntervalMin = 0.4f;
    public float spawnRampTime = 300f;

    [Header("XP & Leveling")]
    public int baseXpToLevel = 20;
    public int xpIncreasePerLevel = 10;
    public int upgradeOptionsCount = 3;

    [Header("Weapon - Fork")]
    public float forkBaseDamage = 15f;
    public float forkBaseCooldown = 0.8f;
    public float forkAttackRange = 5f;
    public float forkLungeSpeed = 25f;

    [Header("Weapon - Boomerang")]
    public float boomerangBaseDamage = 12f;
    public float boomerangBaseCooldown = 1.2f;
    public float boomerangRange = 8f;
    public float boomerangSpeed = 18f;
    public float boomerangHitRadius = 1f;

    [Header("Weapon - JellyBean")]
    public float jellyBeanBaseDamage = 6f;
    public float jellyBeanBaseCooldown = 0.5f;
    public float jellyBeanSpeed = 12f;
    public int jellyBeanCount = 1;
    public float jellyBeanSpread = 10f;

    [Header("Weapon - Bomb")]
    public float bombBaseDamage = 60f;
    public float bombBaseCooldown = 5f;
    public float bombRange = 7f;
    public float bombThrowSpeed = 8f;
    public float bombExplosionRadius = 4f;

    [Header("Boss")]
    public float bossSpawnInterval = 300f;      // boss slot every 5 minutes
    public float bossWarningLead = 5f;         // banner shows this long before the slot
    public float bossBaseHealth = 2000f;
    public float bossHealthScalePerSpawn = 0.9f; // +90% health each later boss
    public float bossMoveSpeed = 1.6f;
    public float bossLaserDamage = 12f;
    public float bossProjectileDamage = 9f;
    public float bossSlamDamage = 22f;
    public int bossXpDrop = 120;

    [Header("Merchant & Evolution Shop")]
    public float merchantSpawnInterval = 180f;  // merchant every 3 minutes
    public float merchantHealth = 90f;          // ~3x a regular enemy
    public float merchantMoveSpeed = 3.5f;      // faster than regular enemies
    public int evolutionCost = 15;              // coins per evolution

    public int XpRequiredForLevel(int level)
    {
        return baseXpToLevel + (level - 1) * xpIncreasePerLevel;
    }

    public float SpawnIntervalAtTime(float time)
    {
        float t = Mathf.Clamp01(time / spawnRampTime);
        return Mathf.Lerp(spawnInterval, spawnIntervalMin, t);
    }

    public float EnemyHealthScaleAtTime(float time)
    {
        return 1f + (time / 60f) * enemyHealthScalePerMinute;
    }

    public float EnemySpeedScaleAtTime(float time)
    {
        return 1f + (time / 60f) * enemySpeedScalePerMinute;
    }

    public float EnemyDamageScaleAtTime(float time)
    {
        return 1f + (time / 60f) * enemyDamageScalePerMinute;
    }
}
