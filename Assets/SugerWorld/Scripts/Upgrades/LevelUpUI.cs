using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private List<UpgradeOptionUI> options = new List<UpgradeOptionUI>();
    [SerializeField] private List<UpgradeData> upgradePool;

    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp.AddListener(OnLevelUp);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp.RemoveListener(OnLevelUp);
    }

    private void OnLevelUp(int level)
    {
        Show();
    }

    private void Show()
    {
        panel.SetActive(true);
        var picks = GetRandomUpgrades(GameManager.Instance.Config.upgradeOptionsCount);

        for (int i = 0; i < options.Count; i++)
        {
            if (i < picks.Count)
            {
                options[i].Setup(picks[i], OnOptionSelected);
                options[i].gameObject.SetActive(true);
            }
            else
            {
                options[i].gameObject.SetActive(false);
            }
        }
    }

    private List<UpgradeData> GetRandomUpgrades(int count)
    {
        int currentLevel = GameManager.Instance.CurrentLevel;
        var pool = new List<UpgradeData>();
        foreach (var u in upgradePool)
        {
            if (u.IsAvailable(currentLevel))
                pool.Add(u);
        }

        // Dynamically add per-weapon upgrade options for weapons the player already owns
        if (PlayerController.Instance != null)
        {
            // For each NewWeapon upgrade that has been unlocked, add a WeaponUpgrade for it
            foreach (var u in upgradePool)
            {
                if (u.type == UpgradeType.NewWeapon && u.alreadyUnlocked && u.weaponPrefab != null)
                {
                    string weaponName = u.weaponPrefab.weaponName;
                    int weaponLevel = PlayerController.Instance.GetWeaponLevel(weaponName);
                    int maxLevel = u.weaponPrefab.maxLevel;

                    // Skip if weapon is already at max level
                    if (weaponLevel >= maxLevel)
                        continue;

                    // Check if a specific weapon upgrade already exists in pool
                    bool exists = false;
                    foreach (var p in pool)
                    {
                        if (p.type == UpgradeType.WeaponUpgrade && p.weaponUpgradeTarget == weaponName)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        var wu = ScriptableObject.CreateInstance<UpgradeData>();
                        wu.upgradeName = $"{weaponName} Lv.{weaponLevel}→{weaponLevel + 1}";
                        wu.description = $"Upgrade {weaponName} (Lv.{weaponLevel}/{maxLevel}) to next level";
                        wu.type = UpgradeType.WeaponUpgrade;
                        wu.weaponUpgradeTarget = weaponName;
                        pool.Add(wu);
                    }
                }
            }

            // Also filter static weapon upgrades (e.g. Fork Upgrade) by max level
            var filtered = new List<UpgradeData>();
            foreach (var p in pool)
            {
                if (p.type == UpgradeType.WeaponUpgrade && !p.upgradesAllWeapons
                    && !string.IsNullOrEmpty(p.weaponUpgradeTarget))
                {
                    int level = PlayerController.Instance.GetWeaponLevel(p.weaponUpgradeTarget);
                    // Find the weapon prefab to get maxLevel
                    int maxLevel = 5; // default
                    foreach (var u in upgradePool)
                    {
                        if (u.type == UpgradeType.NewWeapon && u.alreadyUnlocked
                            && u.weaponPrefab != null && u.weaponPrefab.weaponName == p.weaponUpgradeTarget)
                        {
                            maxLevel = u.weaponPrefab.maxLevel;
                            break;
                        }
                    }
                    // Also check fork (always owned)
                    if (p.weaponUpgradeTarget == "Fork")
                    {
                        // Fork maxLevel is 5 from WeaponBase
                        var forkWeapon = PlayerController.FindWeaponByName(PlayerController.Instance, "Fork");
                        if (forkWeapon != null)
                        {
                            level = forkWeapon.currentLevel;
                            maxLevel = forkWeapon.maxLevel;
                        }
                    }

                    if (level >= maxLevel)
                        continue; // Skip this upgrade

                    // Update the display name with level info
                    p.upgradeName = $"{p.weaponUpgradeTarget} Lv.{level}→{level + 1}";
                    p.description = $"Upgrade {p.weaponUpgradeTarget} (Lv.{level}/{maxLevel}) to next level";
                }
                filtered.Add(p);
            }
            pool = filtered;
        }

        var result = new List<UpgradeData>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }

    private void OnOptionSelected(UpgradeData upgrade)
    {
        if (upgrade != null && PlayerController.Instance != null)
        {
            upgrade.Apply(PlayerController.Instance);
        }
        panel.SetActive(false);
        GameManager.Instance?.SetState(GameState.Playing);
        GameManager.Instance?.ProcessPendingLevelUps();
    }

    [System.Serializable]
    public class UpgradeOptionUI
    {
        public GameObject gameObject;
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public Button button;

        private UpgradeData data;
        private System.Action<UpgradeData> callback;

        public void Setup(UpgradeData upgrade, System.Action<UpgradeData> onSelect)
        {
            data = upgrade;
            callback = onSelect;
            if (icon != null && upgrade.icon != null)
                icon.sprite = upgrade.icon;
            if (nameText != null)
                nameText.text = upgrade.upgradeName;
            if (descriptionText != null)
                descriptionText.text = upgrade.GetDescription();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => callback?.Invoke(data));
            }
        }
    }
}
