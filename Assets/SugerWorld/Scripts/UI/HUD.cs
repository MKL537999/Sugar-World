using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image xpBarFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text killText;

    private PlayerController player;

    private void Start()
    {
        player = PlayerController.Instance;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnXpChanged.AddListener(_ => UpdateXP());
            GameManager.Instance.OnCoinsChanged.AddListener(_ => UpdateCoins());
            GameManager.Instance.OnLevelUp.AddListener(_ => UpdateLevel());
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        UpdateHealth();
        UpdateTime();
        UpdateKills();
    }

    private void UpdateHealth()
    {
        if (player == null) return;
        float maxHp = player.MaxHealth;
        if (maxHp <= 0f) maxHp = 100f;
        float ratio = Mathf.Clamp01(player.CurrentHealth / maxHp);

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = ratio;
            healthBarFill.color = ratio > 0.5f
                ? new Color(0.3f, 0.85f, 0.4f)
                : ratio > 0.25f
                    ? new Color(1f, 0.75f, 0.2f)
                    : new Color(0.85f, 0.2f, 0.2f);
        }
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(player.CurrentHealth)}/{Mathf.CeilToInt(maxHp)}";
            healthText.color = new Color(0.3f, 0.6f, 0.9f, 1f);
        }
    }

    private void UpdateXP()
    {
        var gm = GameManager.Instance;
        float ratio = (float)gm.CurrentXp / gm.XpToNextLevel;
        if (xpBarFill != null)
            xpBarFill.fillAmount = ratio;
        if (xpText != null)
            xpText.text = $"{gm.CurrentXp}/{gm.XpToNextLevel}";
    }

    private void UpdateLevel()
    {
        if (levelText != null)
            levelText.text = $"Lv.{GameManager.Instance.CurrentLevel}";
    }

    private void UpdateTime()
    {
        if (timeText == null) return;
        int totalSeconds = Mathf.FloorToInt(GameManager.Instance.SurviveTime);
        int min = totalSeconds / 60;
        int sec = totalSeconds % 60;
        timeText.text = $"{min:D2}:{sec:D2}";
    }

    private void UpdateCoins()
    {
        if (coinText != null)
            coinText.text = GameManager.Instance.TotalCoins.ToString();
    }

    private void UpdateKills()
    {
        if (killText != null)
            killText.text = $"Kills: {GameManager.Instance.TotalKills}";
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnXpChanged.RemoveAllListeners();
            GameManager.Instance.OnCoinsChanged.RemoveAllListeners();
            GameManager.Instance.OnLevelUp.RemoveAllListeners();
        }
    }
}
