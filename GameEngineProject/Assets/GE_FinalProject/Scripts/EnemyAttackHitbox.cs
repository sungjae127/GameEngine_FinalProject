using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    [SerializeField] private int attackDamage = 10;

    private Collider2D hitboxCollider;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        // 활성화될 때 이미 겹쳐있는 플레이어를 감지
        if (hitboxCollider != null)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                hitboxCollider.bounds.center,
                hitboxCollider.bounds.size,
                0f
            );

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    DealDamage(hit);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어를 때렸을 때
        if (other.CompareTag("Player"))
        {
            DealDamage(other);
        }
    }

    private void DealDamage(Collider2D other)
    {
        Debug.Log("Enemy hit Player: " + other.name);

        // 플레이어에게 데미지 전달
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(attackDamage);
        }
    }
}
