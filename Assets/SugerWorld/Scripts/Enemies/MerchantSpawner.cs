using TMPro;
using UnityEngine;

// Spawns the ginger-bear merchant every few minutes at a random map location.
// While one merchant is alive the next slot is skipped. A short notice banner
// tells the player the merchant has arrived.
public class MerchantSpawner : MonoBehaviour
{
    public Merchant merchantTemplate;
    public float spawnInterval = 180f;   // overridden from GameConfig
    public TMP_Text noticeLabel;

    private float nextSpawnTime;
    private float holdTimer;
    private Merchant currentMerchant;

    private static readonly Color NoticeColor = new Color(1f, 0.78f, 0.3f);

    private void Start()
    {
        nextSpawnTime = spawnInterval;
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameState.Playing) return;

        float t = gm.SurviveTime;

        // Auto-hide the arrival notice
        if (noticeLabel != null && noticeLabel.gameObject.activeSelf)
        {
            holdTimer -= Time.deltaTime;
            if (holdTimer <= 0f) noticeLabel.gameObject.SetActive(false);
        }

        if (t < nextSpawnTime) return;

        nextSpawnTime += spawnInterval;

        bool alive = currentMerchant != null && currentMerchant.IsAlive;
        if (alive) return;   // previous merchant still wandering — skip this slot

        SpawnMerchant();
        ShowNotice("GINGER-BEAR MERCHANT ROAMS THE MAP! DEFEAT IT TO OPEN THE EVOLUTION SHOP", 4f);
    }

    private void SpawnMerchant()
    {
        if (merchantTemplate == null) return;

        Vector3 pos = new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0f);
        var m = Instantiate(merchantTemplate, pos, Quaternion.identity);
        m.gameObject.SetActive(true);
        m.Init(1f, 1f, 1f);
        currentMerchant = m;
    }

    private void ShowNotice(string text, float hold)
    {
        if (noticeLabel == null) return;
        noticeLabel.text = text;
        noticeLabel.color = NoticeColor;
        noticeLabel.gameObject.SetActive(true);
        holdTimer = hold;
    }
}
