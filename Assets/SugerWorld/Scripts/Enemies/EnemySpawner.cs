using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<Enemy> enemyPrefabs = new List<Enemy>();
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Boss Fight Throttling")]
    [Tooltip("Spawn interval multiplier while a boss is alive")]
    [SerializeField] private float bossFightSpawnIntervalMult = 2.5f;
    [SerializeField] private int bossFightMaxAlive = 60;

    private float spawnTimer;
    private GameConfig config;
    private int enemiesAlive;
    private BossSpawner bossSpawner;

    private void Start()
    {
        config = GameManager.Instance?.Config;
        if (config != null)
            spawnTimer = config.spawnInterval;
        bossSpawner = FindObjectOfType<BossSpawner>();
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        spawnTimer -= Time.deltaTime;

        // While a boss is alive the horde is kept small so the fight has room to breathe
        bool bossFight = bossSpawner != null && bossSpawner.BossAlive;
        int cap = bossFight ? bossFightMaxAlive : config.maxEnemiesAlive;

        if (spawnTimer <= 0f && enemiesAlive < cap)
        {
            float interval = config.SpawnIntervalAtTime(GameManager.Instance.SurviveTime);
            if (bossFight) interval *= bossFightSpawnIntervalMult;
            spawnTimer = interval;
            SpawnEnemy();
        }

        // Track alive count
        enemiesAlive = FindObjectsOfType<Enemy>().Length;
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Count == 0) return;

        // Categorize prefabs by attack type
        var meleePrefabs = new List<Enemy>();
        var rangedPrefabs = new List<Enemy>();
        var chargerPrefabs = new List<Enemy>();
        foreach (var p in enemyPrefabs)
        {
            switch (p.attackType)
            {
                case EnemyAttackType.Melee: meleePrefabs.Add(p); break;
                case EnemyAttackType.Ranged: rangedPrefabs.Add(p); break;
                case EnemyAttackType.Charger: chargerPrefabs.Add(p); break;
            }
        }

        Enemy prefab = null;
        int playerLevel = GameManager.Instance.CurrentLevel;

        if (playerLevel <= 3)
        {
            // First 3 levels: only melee enemies
            if (meleePrefabs.Count > 0)
                prefab = meleePrefabs[Random.Range(0, meleePrefabs.Count)];
        }
        else
        {
            // Weighted ratio melee : ranged : charger = 6 : 3 : 1
            int roll = Random.Range(0, 10);
            if (roll < 6 && meleePrefabs.Count > 0)
                prefab = meleePrefabs[Random.Range(0, meleePrefabs.Count)];
            else if (roll < 9 && rangedPrefabs.Count > 0)
                prefab = rangedPrefabs[Random.Range(0, rangedPrefabs.Count)];
            else if (chargerPrefabs.Count > 0)
                prefab = chargerPrefabs[Random.Range(0, chargerPrefabs.Count)];
        }

        if (prefab == null)
            prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        Vector3 spawnPos = GetSpawnPosition();

        var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        enemy.gameObject.SetActive(true);
        float time = GameManager.Instance.SurviveTime;
        enemy.Init(config.EnemyHealthScaleAtTime(time), config.EnemySpeedScaleAtTime(time), config.EnemyDamageScaleAtTime(time));
    }

    private Vector3 GetSpawnPosition()
    {
        if (PlayerController.Instance == null)
            return Random.insideUnitCircle * config.spawnDistanceMin;

        Vector3 playerPos = PlayerController.Instance.transform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(config.spawnDistanceMin, config.spawnDistanceMax);
        return playerPos + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * dist;
    }

    // Sweeps a fraction of the horde when the boss appears (silent despawn — no drops, no kill count)
    public void ThinHerd(float fraction)
    {
        var all = FindObjectsOfType<Enemy>();
        int target = Mathf.CeilToInt(all.Length * Mathf.Clamp01(fraction));
        int removed = 0;
        foreach (var e in all)
        {
            if (removed >= target) break;
            if (e is Boss) continue;        // never sweep the boss itself
            if (e is Merchant) continue;    // never sweep the merchant
            Destroy(e.gameObject);
            removed++;
        }
        enemiesAlive = 0;   // recounted on the next Update
    }
}
