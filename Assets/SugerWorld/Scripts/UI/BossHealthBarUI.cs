using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Boss health bar shown at the top-center of the screen while a boss is alive.
// Built entirely by GameBootstrap; polls the bound boss's health each frame.
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    public GameObject panel;
    public Image fill;
    public TMP_Text label;

    private Boss bound;

    private void Awake()
    {
        Instance = this;
    }

    public void Bind(Boss boss)
    {
        bound = boss;
        if (panel != null) panel.SetActive(true);
    }

    public void Unbind(Boss boss)
    {
        if (bound != boss) return;
        bound = null;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (bound == null) return;

        float max = Mathf.Max(1f, bound.maxHealth);
        float ratio = Mathf.Clamp01(bound.CurrentHealth / max);
        if (fill != null)
            fill.fillAmount = ratio;
        if (label != null)
            label.text = $"VEND-O-MATIC  {Mathf.CeilToInt(bound.CurrentHealth)}/{Mathf.CeilToInt(max)}";
    }
}
