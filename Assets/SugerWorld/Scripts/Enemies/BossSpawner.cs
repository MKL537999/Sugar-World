using TMPro;
using UnityEngine;

// Spawns the vending-machine boss every 5 minutes at a fixed map location.
// A flashing banner warns the player shortly before each boss slot.
// If the previous boss is still alive when the next slot arrives, that slot is skipped.
public class BossSpawner : MonoBehaviour
{
    public Boss bossTemplate;
    public Vector3 spawnLocation = new Vector3(0f, 12f, 0f);
    public float spawnInterval = 300f;      // overridden from GameConfig
    public float warningLead = 5f;
    public float healthScalePerSpawn = 0.9f;
    public TMP_Text warningLabel;

    private float nextBossTime;
    private float holdTimer;
    private bool warningShown;
    private int spawnCount;
    private Boss currentBoss;

    public bool BossAlive => currentBoss != null && currentBoss.IsAlive;

    private static readonly Color WarningRed = new Color(1f, 0.25f, 0.2f);
    private static readonly Color WarningYellow = new Color(1f, 0.85f, 0.2f);

    private void Start()
    {
        nextBossTime = spawnInterval;
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameState.Playing) return;

        float t = gm.SurviveTime;
        bool bossAlive = currentBoss != null && currentBoss.IsAlive;

        // Show the incoming warning a few seconds before each boss slot
        if (!bossAlive && !warningShown && t >= nextBossTime - warningLead)
        {
            warningShown = true;
            ShowBanner("!! VEND-O-MATIC INCOMING !!", flash: true);
        }

        if (warningLabel != null && warningLabel.gameObject.activeSelf)
        {
            if (holdTimer > 0f)
            {
                holdTimer -= Time.deltaTime;
                warningLabel.color = WarningYellow;
                if (holdTimer <= 0f && !(t >= nextBossTime - warningLead))
                    HideBanner();
            }
            else
            {
                // Flashing red pulse while the warning is active
                float pulse = (Mathf.Sin(Time.time * 10f) + 1f) * 0.5f;
                warningLabel.color = Color.Lerp(WarningRed, WarningYellow, pulse);
            }
        }

        if (t >= nextBossTime)
        {
            nextBossTime += spawnInterval;

            if (bossAlive)
            {
                // Previous boss still fighting — skip this slot
                warningShown = false;
                HideBanner();
            }
            else
            {
                SpawnBoss();
                warningShown = false;
                ShowBanner("!! VEND-O-MATIC APPEARED !!", flash: false);
                holdTimer = 2.5f;
            }
        }
    }

    private void SpawnBoss()
    {
        if (bossTemplate == null) return;

        var boss = Instantiate(bossTemplate, spawnLocation, Quaternion.identity);
        boss.gameObject.SetActive(true);
        int generation = spawnCount;
        spawnCount++;
        float scale = 1f + generation * healthScalePerSpawn;
        // Scale maxHealth itself so the health bar reads e.g. "1900/1900" on later bosses
        // instead of current > max; Init(1,...) then just copies it into currentHealth
        boss.maxHealth *= scale;
        boss.Init(1f, 1f, 1f);
        // Generation 0 is the first boss; later ones attack harder and faster
        boss.ConfigureForGeneration(generation);
        currentBoss = boss;

        // Sweep part of the horde so the fight has room to breathe
        var enemySpawner = FindObjectOfType<EnemySpawner>();
        if (enemySpawner != null)
            enemySpawner.ThinHerd(0.5f);
    }

    private void ShowBanner(string text, bool flash)
    {
        if (warningLabel == null) return;
        warningLabel.text = text;
        warningLabel.color = WarningRed;
        warningLabel.gameObject.SetActive(true);
        holdTimer = flash ? 0f : 2.5f;
    }

    private void HideBanner()
    {
        if (warningLabel != null)
            warningLabel.gameObject.SetActive(false);
    }
}
