using UnityEditor;
using UnityEngine;

public class SceneSetupEditor : EditorWindow
{
    [MenuItem("Sugar World/Setup Scene")]
    public static void SetupScene()
    {
        // Clear existing GameBootstrap if any
        var existing = FindObjectOfType<GameBootstrap>();
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        // Create the bootstrap GameObject
        var go = new GameObject("GameBootstrap");
        var bootstrap = go.AddComponent<GameBootstrap>();

        // Try to auto-assign sprites from the project
        bootstrap.marshalSprite = FindSprite("MarshalSpriteSheet_0");
        bootstrap.forkSprite = FindSpriteByPath("Assets/SugerWorld/Art/Environment/Fork.png");
        bootstrap.boomerangSprite = FindSpriteByPath("Assets/SugerWorld/Art/Environment/DonutPink.png");
        bootstrap.jellyBeanSprite = FindSpriteByPath("Assets/SugerWorld/Art/Environment/Jellybeans.png");
        bootstrap.bombSprite = FindSpriteByPath("Assets/SugerWorld/Art/Environment/Gulaabjamun.png");
        bootstrap.floorSprite = FindSpriteByPath("Assets/SugerWorld/Art/Environment/FloorChocolateSquareCorners.png");

        // Collectible drop sprites
        bootstrap.coinDropSprite = FindSpriteByPath("Assets/SugerWorld/Art/VFX/CoinBullet.png");
        bootstrap.xpDropSprite = FindSpriteByPath("Assets/SugerWorld/Art/VFX/CollectibleCherry.png");
        bootstrap.healthPackSprite = FindSpriteByPath("Assets/SugerWorld/Art/VFX/CollectibleHealth.png");

        // UI sprites
        bootstrap.healthBarFrameSprite = FindSpriteByPath("Assets/SugerWorld/Art/UI/UIHealthFrameBlue.png");
        bootstrap.healthBarFillSprite = FindSpriteByPath("Assets/SugerWorld/Art/UI/UIHealthBarPink.png");
        bootstrap.xpBarFrameSprite = FindSpriteByPath("Assets/SugerWorld/Art/UI/UIDialogueBoxBlue.png");
        bootstrap.coinIconSprite = FindSpriteByPath("Assets/SugerWorld/Art/UI/UICoinIcon.png");
        bootstrap.portraitSprite = FindSpriteByPath("Assets/SugerWorld/Art/UI/CharacterPortraitMarshal.png");

        // Find enemy sprites
        bootstrap.enemySprites = new System.Collections.Generic.List<Sprite>();
        AddIfFound(bootstrap.enemySprites, "Assets/SugerWorld/Art/Environment/DonutPink.png");
        AddIfFound(bootstrap.enemySprites, "Assets/SugerWorld/Art/Environment/DonutYellow.png");
        AddIfFound(bootstrap.enemySprites, "Assets/SugerWorld/Art/Environment/Jellies.png");
        AddIfFound(bootstrap.enemySprites, "Assets/SugerWorld/Art/Environment/Jellybeans.png");
        AddIfFound(bootstrap.enemySprites, "Assets/SugerWorld/Art/Environment/CakeChocBall.png");
        AddIfFound(bootstrap.enemySprites, "Assets/SugerWorld/Art/Environment/CakeCocoBall.png");

        // Map decoration sprites (environment props, not enemies)
        bootstrap.decorationSprites = new System.Collections.Generic.List<Sprite>();
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/HouseGingerbread.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/TreeIceCream99.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/TreeIceCreams.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/TreeMintChocChip.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/CakeSlice.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/CakeSliceTall.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/CakeMacaron.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/CakeMoonCake.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/CakePistachioSlice.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/Baclava.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/Gulaabjamun.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/CreamCherry.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/SprinklesChoc.png");
        AddIfFound(bootstrap.decorationSprites, "Assets/SugerWorld/Art/Environment/Shadow.png");

        Debug.Log("[Sugar World] Scene setup complete! Press Play to start.");
    }

    private static Sprite FindSprite(string name)
    {
        var guids = AssetDatabase.FindAssets($"t:Sprite {name}");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
        }
        return null;
    }

    private static Sprite FindSpriteByPath(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void AddIfFound(System.Collections.Generic.List<Sprite> list, string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) list.Add(sprite);
    }
}
