# Sugar World

A candy-themed 2D survival arena game inspired by *Vampire Survivors*, built with the Tuanjie engine (团结引擎).

Survive endless waves of dessert enemies in a chocolate-tiled arena. Move to dodge, let your weapons auto-attack the nearest enemy, collect XP and coins, and build out your loadout as the threats scale — culminating in the **VEND-O-MATIC** boss every few minutes.

## Gameplay

- Top-down arena survival — there is no "win" condition; survive as long as you can.
- Weapons **auto-aim and auto-fire** at the nearest enemy; you only control movement.
- Enemies drop **XP orbs** (to level up) and **coins** (to spend in the Evolution Shop).
- Each level-up pauses the game and lets you pick **1 of 3 upgrades**.
- Enemies scale in health, speed, and damage over time.

## Controls

| Input | Action |
| --- | --- |
| `W` `A` `S` `D` / arrow keys | Move |

Weapons fire automatically — no aiming or attack input required.

## Weapons

| Weapon | Type | Unlock |
| --- | --- | --- |
| Fork | Melee lunge | Starter |
| Donut Boomerang | Spinning return | ~level 5 |
| Jelly Bean Blaster | Rapid ranged | ~level 5 |
| Gulaab Bomb | AoE explosion | ~level 10 |

## Enemies & Bosses

- **Donut Pink / Donut Yellow** — charge straight at the player.
- **Cake Choc Ball / Cake Coco Ball** — keep distance and lob projectiles.
- **Jellies / Jellybeans** — basic melee contact damage.
- **VEND-O-MATIC** (boss) — a vending-machine robot appearing every ~5 minutes, using laser, projectile, and slam attacks. Each later boss spawns with +90% health.
- **Ginger Bear Merchant** — a neutral NPC that wanders in every ~3 minutes. Defeat it to open the Evolution Shop.

## Upgrades

Level-up choices include stat boosts (Move Speed, Max Health, Pickup Range, Damage, Cooldown), weapon upgrades, and new weapon unlocks.

The **Evolution Shop** (opened by defeating the merchant) lets you spend collected coins to evolve your weapons further.

## Requirements

- **Tuanjie (团结引擎) 1.8.3** — based on Unity **2022.3.62t5**
- 2D + Universal Render Pipeline (URP) packages (see `Packages/manifest.json`)

> **Note:** the manifest references Codely tooling packages (`cn.tuanjie.codely.bridge`, `cn.tuanjie.ai.generators`). The generator package uses a `file:` path pointing outside this repo, so a fresh clone needs the Codely IDE extension installed to resolve packages.

## Getting started

1. Open the project in Tuanjie 1.8.3 (or Unity 2022.3.62t5).
2. Open the scene `Assets/Scenes/SampleScene.scene`.
3. If the scene has no `GameBootstrap` object, run the menu **Sugar World → Setup Scene** to create and auto-wire it.
4. Press **Play**.

The entire game is built at runtime by `GameBootstrap.Awake()` — there are no prefabs or manually-placed objects to set up.

## Project structure

```
Assets/
├── Scenes/                # SampleScene
├── SugerWorld/
│   ├── Art/               # Sprites, tiles, UI, VFX
│   ├── Scripts/
│   │   ├── Core/          # GameBootstrap, GameManager, ObjectPool, SceneSetupEditor
│   │   ├── Player/        # PlayerController
│   │   ├── Combat/        # WeaponBase + weapon types
│   │   ├── Enemies/       # Enemy, Boss, Merchant + spawners
│   │   ├── Collectibles/  # XPOrb, Coin, HealthPack
│   │   ├── Upgrades/      # LevelUpUI, UpgradeData
│   │   ├── UI/            # HUD, GameOver, EvolutionShop, BossHealthBar
│   │   └── Data/          # GameConfig (ScriptableObject)
│   └── Settings/
└── TextMesh Pro/
```
