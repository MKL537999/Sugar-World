using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text surviveTimeText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver.AddListener(Show);

        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);
    }

    private void Show()
    {
        // Hide level-up panel if it's open
        var levelUpPanel = GameObject.Find("LevelUpPanel");
        if (levelUpPanel != null) levelUpPanel.SetActive(false);

        panel.SetActive(true);

        // Set up restart button here — Awake runs before reflection sets the field
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(Restart);
        }

        var gm = GameManager.Instance;
        int totalSeconds = Mathf.FloorToInt(gm.SurviveTime);
        int min = totalSeconds / 60;
        int sec = totalSeconds % 60;

        if (surviveTimeText != null)
            surviveTimeText.text = $"{min:D2}:{sec:D2}";
        if (killsText != null)
            killsText.text = gm.TotalKills.ToString();
        if (coinsText != null)
            coinsText.text = gm.TotalCoins.ToString();
        if (levelText != null)
            levelText.text = gm.CurrentLevel.ToString();
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver.RemoveListener(Show);
    }
}
