using UnityEngine;

public class HealthPack : MonoBehaviour
{
    public float healPercent = 0.08f;
    public float magnetSpeed = 8f;

    private Transform player;
    private bool isMagnetized;

    private void Start()
    {
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
            magnetSpeed = PlayerController.Instance.PickupMagnetSpeed;
        }
        // Config value wins when available (8% of max HP, up from 5%)
        if (GameManager.Instance != null && GameManager.Instance.Config != null)
            healPercent = GameManager.Instance.Config.healthPackHealPercent;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        // Health packs have a smaller pickup range than coins/XP (half the player's pickup range)
        float magnetRange = (PlayerController.Instance?.PickupRange ?? 3f) * 0.5f;

        if (dist <= magnetRange)
            isMagnetized = true;

        if (isMagnetized)
        {
            float speed = Mathf.Lerp(magnetSpeed, magnetSpeed * 2f, 1f - dist / magnetRange);
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);

            if (dist < 0.3f)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        PlayerController.Instance?.Heal(PlayerController.Instance.MaxHealth * healPercent);
        Destroy(gameObject);
    }
}
