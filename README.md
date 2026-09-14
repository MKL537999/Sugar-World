# Sugar World · 糖果世界

一款糖果主题的 2D 生存竞技场游戏，灵感来自《吸血鬼幸存者》(Vampire Survivors)，使用**团结引擎 (Tuanjie)** 开发。

在巧克力地砖铺成的竞技场中，抵御一波波甜点敌人的进攻。移动走位躲避，武器自动攻击最近的敌人，收集经验与金币，不断强化自己的配置，直到面对每隔几分钟现身的 **VEND-O-MATIC** Boss。

---

## 玩法

- 俯视角竞技场生存玩法——没有“胜利”条件，尽可能存活更久。
- 武器会**自动瞄准并攻击最近的敌人**，你只需要控制移动。
- 敌人掉落**经验球**（用于升级）和**金币**（用于进化商店）。
- 每次升级会暂停游戏，让你从 **3 个强化中选择 1 个**。
- 敌人的生命、速度、伤害会随时间逐渐增强。

## 操作

| 输入 | 动作 |
| --- | --- |
| `W` `A` `S` `D` / 方向键 | 移动 |

武器自动开火——无需手动瞄准或攻击。

## 武器

| 武器 | 类型 | 解锁 |
| --- | --- | --- |
| 叉子 (Fork) | 近战突刺 | 初始 |
| 甜甜圈回旋镖 (Donut Boomerang) | 旋转返回 | 约 5 级 |
| 软糖豆射手 (Jelly Bean Blaster) | 快速远程 | 约 5 级 |
| 古拉布炸弹 (Gulaab Bomb) | 范围爆炸 | 约 10 级 |

## 敌人与 Boss

- **粉色/黄色甜甜圈 (Donut Pink / Yellow)** —— 直接冲向玩家。
- **巧克力球蛋糕 / 可可球蛋糕 (Cake Choc / Coco Ball)** —— 保持距离并投掷弹幕。
- **果冻 / 软糖豆 (Jellies / Jellybeans)** —— 基础近战碰撞伤害。
- **VEND-O-MATIC**（Boss）—— 售货机机器人，约每 5 分钟出现一次，使用激光、弹幕与砸地攻击，后续每只 Boss 生命 +90%。
- **姜饼熊商人 (Ginger Bear Merchant)** —— 中立 NPC，约每 3 分钟出现，击败它可打开进化商店。

## 升级

升级三选一包含属性强化（移速、最大生命、拾取范围、伤害、冷却缩减）、武器升级与新武器解锁。

**进化商店**（击败商人开启）可用收集的金币进一步进化武器。

## 环境要求

- **团结引擎 (Tuanjie) 1.8.3** —— 基于 Unity **2022.3.62t5**
- 2D + 通用渲染管线 (URP) 相关包（见 `Packages/manifest.json`）

> **注意：** manifest 中引用了 Codely 工具包（`cn.tuanjie.codely.bridge`、`cn.tuanjie.ai.generators`）。其中生成器包使用了指向仓库外的 `file:` 路径，全新 clone 后需安装 Codely IDE 扩展才能解析依赖。

## 快速开始

1. 用团结引擎 1.8.3（或 Unity 2022.3.62t5）打开项目。
2. 打开场景 `Assets/Scenes/SampleScene.scene`。
3. 如果场景中没有 `GameBootstrap` 对象，运行菜单 **Sugar World → Setup Scene** 自动创建并配置。
4. 点击 **Play**。

整个游戏由 `GameBootstrap.Awake()` 在运行时动态构建——无需任何预制体或手动摆放的对象。

## 项目结构

```
Assets/
├── Scenes/                # SampleScene
├── SugerWorld/
│   ├── Art/               # 精灵图、瓦片、UI、特效
│   ├── Scripts/
│   │   ├── Core/          # GameBootstrap, GameManager, ObjectPool, SceneSetupEditor
│   │   ├── Player/        # PlayerController
│   │   ├── Combat/        # WeaponBase + 各类武器
│   │   ├── Enemies/       # Enemy, Boss, Merchant + 生成器
│   │   ├── Collectibles/  # XPOrb, Coin, HealthPack
│   │   ├── Upgrades/      # LevelUpUI, UpgradeData
│   │   ├── UI/            # HUD, GameOver, EvolutionShop, BossHealthBar
│   │   └── Data/          # GameConfig (ScriptableObject)
│   └── Settings/
└── TextMesh Pro/
```

---

# Sugar World · 糖果世界

A candy-themed 2D survival arena game inspired by *Vampire Survivors*, built with the **Tuanjie engine (团结引擎)**.

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
