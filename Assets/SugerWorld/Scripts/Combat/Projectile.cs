using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public float lifetime = 3f;
    public bool penetrating;

    private Vector2 direction;
    private float timer;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        timer = lifetime;
    }

    public void Fire(Vector2 dir, float dmg, float spd)
    {
        direction = dir.normalized;
        damage = dmg;
        speed = spd;
        timer = lifetime;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }
        rb.velocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.TakeDamage(damage);
            if (!penetrating)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnBecameInvisible()
    {
        gameObject.SetActive(false);
    }
}
