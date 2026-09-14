using UnityEngine;

public class Coin : MonoBehaviour
{
    public int coinAmount = 1;
    public float magnetSpeed = 8f;

    private Transform player;
    private bool isMagnetized;
    private Rigidbody2D rb;

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
            magnetSpeed = PlayerController.Instance.PickupMagnetSpeed;
        }

        Vector2 dir = Random.insideUnitCircle.normalized * 3f;
        rb.velocity = dir;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float magnetRange = PlayerController.Instance?.PickupRange ?? 3f;

        if (dist <= magnetRange)
            isMagnetized = true;

        if (isMagnetized)
        {
            Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
            float speed = Mathf.Lerp(magnetSpeed, magnetSpeed * 2f, 1f - dist / magnetRange);
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);

            if (dist < 0.3f)
            {
                GameManager.Instance?.AddCoins(coinAmount);
                Destroy(gameObject);
            }
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * 2f);
        }
    }
}
