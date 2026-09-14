using UnityEngine;

public class XPOrb : MonoBehaviour
{
    public int xpAmount = 5;
    public float magnetRange = 3f;
    public float magnetSpeed = 8f;
    public float moveSpeed = 3f;

    private Transform player;
    private Rigidbody2D rb;
    private Vector2 initialDirection;
    private bool isMagnetized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
            magnetRange = PlayerController.Instance.PickupRange;
            magnetSpeed = PlayerController.Instance.PickupMagnetSpeed;
        }

        initialDirection = Random.insideUnitCircle.normalized * moveSpeed;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= magnetRange)
        {
            isMagnetized = true;
        }

        if (isMagnetized)
        {
            Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
            float speed = Mathf.Lerp(magnetSpeed, magnetSpeed * 2f, 1f - dist / magnetRange);
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);

            if (dist < 0.3f)
            {
                Collect();
            }
        }
        else
        {
            // Drift outward then slow down
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * 2f);
        }
    }

    private void Collect()
    {
        GameManager.Instance?.AddXp(xpAmount);
        Destroy(gameObject);
    }
}
