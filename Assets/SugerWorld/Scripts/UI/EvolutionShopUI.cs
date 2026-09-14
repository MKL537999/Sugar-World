using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Coin-based weapon evolution shop, opened by defeating the ginger-bear merchant.
// Presents up to three evolutions for weapons the player already owns; the
// player may buy any subset of them (0-3) or close without buying anything.
public class EvolutionShopUI : MonoBehaviour
{
    public static EvolutionShopUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text coinLabel;
    [SerializeField] private List<ShopOptionUI> options = new List<ShopOptionUI>();

    private readonly List<Offer> currentOffers = new List<Offer>();
    private int costPerEvolution = 20;

    private class Offer
    {
        public string name;
        public string description;
        public Sprite icon;
        public Action apply;   // null once purchased
    }

    [System.Serializable]
    public class ShopOptionUI
    {
        public GameObject gameObject;
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public TMP_Text costText;
        public Button button;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        var cfg = gm.Config;
        if (cfg != null) costPerEvolution = cfg.evolutionCost;

        currentOffers.Clear();
        currentOffers.AddRange(BuildOffers());
        // Randomly keep at most one offer per card
        while (currentOffers.Count > options.Count)
            currentOffers.RemoveAt(UnityEngine.Random.Range(0, currentOffers.Count));

        gm.SetState(GameState.Shopping);
        Refresh();
        if (panel != null) panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.SetState(GameState.Playing);
        gm.ProcessPendingLevelUps();
    }

    // Called by the card buttons (wired by GameBootstrap)
    public void OnOptionClicked(int index)
    {
        if (index < 0 || index >= currentOffers.Count) return;
        var offer = currentOffers[index];
        if (offer.apply == null) return;   // already sold

        var gm = GameManager.Instance;
        if (gm == null || gm.TotalCoins < costPerEvolution) return;

        gm.AddCoins(-costPerEvolution);
        offer.apply();
        offer.apply = null;   // mark as sold
        Refresh();
    }

    private void Refresh()
    {
        var gm = GameManager.Instance;
        int coins = gm != null ? gm.TotalCoins : 0;

        for (int i = 0; i < options.Count; i++)
        {
            var ui = options[i];
            if (ui == null) continue;

            bool visible = i < currentOffers.Count;
            ui.gameObject.SetActive(visible);
            if (!visible) continue;

            var offer = currentOffers[i];
            bool sold = offer.apply == null;
            bool affordable = coins >= costPerEvolution;

            if (ui.icon != null) ui.icon.sprite = offer.icon;
            if (ui.nameText != null)
                ui.nameText.text = sold ? offer.name + "  (SOLD)" : offer.name;
            if (ui.descriptionText != null)
                ui.descriptionText.text = offer.description;
            if (ui.costText != null)
            {
                ui.costText.text = sold ? "—" : costPerEvolution + " coins";
                ui.costText.color = sold || affordable
                    ? new Color(1f, 0.85f, 0.3f)
                    : new Color(1f, 0.35f, 0.3f);
            }
            if (ui.button != null)
                ui.button.interactable = !sold && affordable;
        }

        if (coinLabel != null)
            coinLabel.text = "Coins: " + coins;
    }

    private List<Offer> BuildOffers()
    {
        var list = new List<Offer>();
        var player = PlayerController.Instance;
        if (player == null) return list;

        var fork = PlayerController.FindWeaponByName(player, "Fork") as WeaponFork;
        if (fork != null)
        {
            if (!fork.isGiant)
                list.Add(new Offer
                {
                    name = "Giant Fork",
                    description = "Fork grows huge: +80% damage, +50% range, slightly slower",
                    icon = fork.icon,
                    apply = fork.EvolveGiant
                });
            list.Add(new Offer
            {
                name = "Orbiting Forks",
                description = fork.orbitCount == 0
                    ? "2 forks orbit you, damaging enemies they touch"
                    : $"+1 orbiting fork ({fork.orbitCount + 1}), spins faster & wider",
                icon = fork.icon,
                apply = fork.EvolveOrbit
            });
        }

        var jelly = PlayerController.FindWeaponByName(player, "Jelly Bean Blaster") as WeaponJellyBean;
        if (jelly != null)
            list.Add(new Offer
            {
                name = "Shotgun Blast",
                description = jelly.isShotgun
                    ? $"Wider spread ({Mathf.Min(180f, jelly.spreadAngle + 20f):0}°, max 180°), +3 pellets, slower rate"
                    : "Fires a tight 5-pellet shotgun cone toward enemies (slower rate)",
                icon = jelly.icon,
                apply = jelly.EvolveShotgun
            });

        var boomerang = PlayerController.FindWeaponByName(player, "Donut Boomerang") as WeaponBoomerang;
        if (boomerang != null)
            list.Add(new Offer
            {
                name = "Multi Boomerang",
                description = boomerang.throwCount == 1
                    ? "Throws 3 boomerangs in multiple directions"
                    : $"Throws {boomerang.throwCount + 2} boomerangs in multiple directions",
                icon = boomerang.icon,
                apply = boomerang.EvolveMulti
            });

        var bomb = PlayerController.FindWeaponByName(player, "Gulaab Bomb") as WeaponBomb;
        if (bomb != null)
            list.Add(new Offer
            {
                name = "Mortar Strike",
                description = bomb.isMortar
                    ? $"+1 bomb per mortar volley ({bomb.mortarCount + 1})"
                    : "Bombs rain from the sky onto the densest enemy cluster",
                icon = bomb.icon,
                apply = bomb.EvolveMortar
            });

        return list;
    }
}
