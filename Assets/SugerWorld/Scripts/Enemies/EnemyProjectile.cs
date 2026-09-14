using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float damage = 5f;
    public float lifetime = 4f;

    private Vector2 direction;
    private float timer;

    public void Fire(Vector2 dir, float dmg, float spd)
    {
        direction = dir.normalized;
        damage = dmg;
        speed = spd;
        timer = lifetime;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerController>(out var player))
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    // NOTE: no OnBecameInvisible kill here — bullets must survive off-screen so
    // multi-wave boss barrages can stack into dense fields. lifetime caps them.
}
