using UnityEngine;

public class HealthPackSpawner : MonoBehaviour
{
    public Sprite healthPackSprite;
    public float spawnInterval = 30f;
    public float firstSpawnTime = 30f;
    public Vector2 spawnPosition = new Vector2(-1f, 0.5f);

    private float timer;

    private void Start()
    {
        timer = firstSpawnTime;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameState.GameOver) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = spawnInterval;
            SpawnHealthPack();
        }
    }

    private void SpawnHealthPack()
    {
        var go = new GameObject("HealthPack");
        go.transform.position = spawnPosition;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        sr.sprite = healthPackSprite;
        go.transform.localScale = Vector3.one * 0.45f;

        var pack = go.AddComponent<HealthPack>();
        pack.healPercent = 0.05f;
    }
}
